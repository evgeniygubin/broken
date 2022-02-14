using BrokenCode.Etc;
using BrokenCode.Interfaces;
using BrokenCode.Repository;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrokenCode
{
    public class FixedService : IReportService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BrokenService));
        private readonly ILicenseServiceProvider _licenseServiceProvider;
        private readonly IUsersRepository _usersRepository;
        private AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private LicenseServiceOptions _licenceServiceOptions;

        public FixedService(IUsersRepository usersRepository, ILicenseServiceProvider licenseServiceProvider, IOptions<LicenseServiceOptions> licenceServiceOptions)
        {
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _licenseServiceProvider = licenseServiceProvider ?? throw new ArgumentNullException(nameof(licenseServiceProvider));
            _licenceServiceOptions = licenceServiceOptions.Value;

            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromSeconds(10)
                );
        }

        public async Task<IActionResult> GetReport(GetReportRequest request)
        {
            try
            {
                var res = await _circuitBreakerPolicy.ExecuteAsync(async () => await GetReportAsync(request));
                if (!res.Succeeded) { return new BadRequestObjectResult(res.Error); }

                return new OkObjectResult(res);
            }
            catch (BrokenCircuitException ex)
            {
                Log.Error($"Circuit is Broken {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                Log.Error($"GetReport Exception {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<ReportResults<UserStatistics>> GetReportAsync(GetReportRequest request)
        {
            var filteredUsers = await _usersRepository.GetBackupEnabledInDomainUsers(request.DomainId, request.PageSize, request.PageNumber);
            if (filteredUsers is null) { return new ReportResults<UserStatistics>() { Succeeded = false, Error = $"Problem of getting Users for {request.DomainId}" }; }

            int totalCount = filteredUsers.Count();
            if (totalCount == 0) { return new ReportResults<UserStatistics>() { Succeeded = false, Error = $"Users for domain '{request.DomainId}' not found" }; }

            using var licenseService = _licenseServiceProvider.GetLicenseService(_licenceServiceOptions.TimeOut);
            if (licenseService is null) { return new ReportResults<UserStatistics>() { Succeeded = false, Error = "Licence service error" }; }

            var licensedUserCount = await licenseService.GetLicensedUserCountAsync(request.DomainId);
            Log.Info($"Total licenses for domain '{request.DomainId}': {licensedUserCount}");

            var userEmails = filteredUsers.Select(u => u.UserEmail).ToList();
            ICollection<LicenseInfo> licesnces = await licenseService.GetLicensesAsync(request.DomainId, userEmails);
            if (licesnces is null) { return new ReportResults<UserStatistics>() { Succeeded = false, Error = $"Problem of getting licenses for {request.DomainId}" }; }

            Dictionary<Guid, LicenseInfo> userLicenses = licesnces.ToDictionary(k => k.UserId);

            var usersData = filteredUsers
                .Select(u =>
                {
                    return new UserStatistics
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        InBackup = u.BackupEnabled,
                        EmailLastBackupStatus = u.Email.LastBackupStatus,
                        EmailLastBackupDate = u.Email.LastBackupDate,
                        DriveLastBackupStatus = u.Drive.LastBackupStatus,
                        DriveLastBackupDate = u.Drive.LastBackupDate,
                        CalendarLastBackupStatus = u.Calendar.LastBackupStatus,
                        CalendarLastBackupDate = u.Calendar.LastBackupDate,
                        LicenseType = userLicenses.ContainsKey(u.Id) ? (userLicenses[u.Id].IsTrial ? "Trial" : "Paid") : "None"
                    };
                });

            return new ReportResults<UserStatistics>()
            {
                Succeeded = true,
                TotalCount = totalCount,
                Data = usersData
            };
        }
    }
}
