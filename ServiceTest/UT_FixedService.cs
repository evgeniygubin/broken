using BrokenCode;
using BrokenCode.Etc;
using BrokenCode.Interfaces;
using BrokenCode.Model;
using BrokenCode.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceTest
{
    [TestFixture]
    public class Tests
    {
        private UserDbContext _context;
        private FixedService _fservice;
        private Mock<ILicenseService> _licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            InitDbContext();

            var repository = new UsersRepository(_context);
            var licenceServiceProviderMock = new Mock<ILicenseServiceProvider>();
            _licenseServiceMock = new Mock<ILicenseService>();
            licenceServiceProviderMock.Setup(x => x.GetLicenseService(It.IsAny<int>())).Returns(_licenseServiceMock.Object);
            _licenseServiceMock.Setup(x => x.GetLicensedUserCountAsync(It.IsAny<Guid>())).ReturnsAsync(123);
            _licenseServiceMock.Setup(x => x.GetLicensesAsync(It.IsAny<Guid>(), It.IsAny<ICollection<string>>())).ReturnsAsync(new List<LicenseInfo> {
                new LicenseInfo { Email = "bill.morgan@contoso.com", IsTrial = true, UserId =  new Guid("{A70227A7-EADE-4179-8A90-B43F9B5AC756}") },
                new LicenseInfo{ Email = "jack.hill@contoso.com", IsTrial = false, UserId =  new Guid("{9B3C584D-8BAD-453C-AA07-8EF5213EC042}") }
            });
            var _options = Options.Create(new LicenseServiceOptions() { TimeOut = 5000 });
            _fservice = new FixedService(repository, licenceServiceProviderMock.Object, _options);
        }

        private void InitDbContext()
        {
            var _contextOptions = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase("TestUserDb")
                .Options;

            _context = new UserDbContext(_contextOptions);

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            var bill = _context.Users.Add(new User { Id = new Guid("{A70227A7-EADE-4179-8A90-B43F9B5AC756}"), DomainId = new Guid("{0B48F0DE-FC95-4B0D-B0DF-08614CA3F6A8}"), UserEmail = "bill.morgan@contoso.com", UserName = "Bill Morgan", BackupEnabled = true, State = UserState.InDomain }).Entity;
            var jack = _context.Users.Add(new User { Id = new Guid("{9B3C584D-8BAD-453C-AA07-8EF5213EC042}"), DomainId = new Guid("{0B48F0DE-FC95-4B0D-B0DF-08614CA3F6A8}"), UserEmail = "jack.hill@contoso.com", UserName = "Jack Hill", BackupEnabled = true, State = UserState.InDomain }).Entity;
            var sam = _context.Users.Add(new User { Id = new Guid("{1E76F64F-D945-4C4F-BDA5-5B95AF15842D}"), DomainId = new Guid("{6F7AFD03-2442-44FA-9A43-4D34EDD681D9}"), UserEmail = "sam.smith@contoso.com", UserName = "Sam Smith", BackupEnabled = true, State = UserState.InDomain }).Entity;
            var ann = _context.Users.Add(new User { Id = new Guid("{02272A80-A99B-4C99-A57A-E0A4CCD38F32}"), DomainId = new Guid("{0B48F0DE-FC95-4B0D-B0DF-08614CA3F6A8}"), UserEmail = "ann.mill@contoso.com", UserName = "Ann Mill", BackupEnabled = true, State = UserState.NotInDomain }).Entity;
            _context.AddRange(
                new Email { Id = new Guid("{741B8694-6E9A-4A38-95C9-500EAA2719E4}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = bill },
                new Email { Id = new Guid("{D17D836A-3C14-4A7F-839B-D403E88CE2F8}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = jack },
                new Email { Id = new Guid("{298CA47C-7EDD-47A7-9AE4-C1B4BFBE05C7}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = sam },
                new Email { Id = new Guid("{84D4BE65-2E7E-4B37-9EF5-D2057C57A369}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = ann },

                new Drive { Id = new Guid("{7158E0F4-AACB-4630-AF7A-BAC7EC1D4F3E}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = bill },
                new Drive { Id = new Guid("{6973C74F-2670-44A0-9365-AB50DE9D756D}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = jack },
                new Drive { Id = new Guid("{AF49EA4D-2F92-4CDE-919E-ADC2E27A7B8D}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = sam },
                new Drive { Id = new Guid("{BC911F13-7480-46BC-B36C-7BEAC5761B0B}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = ann },

                new Calendar { Id = new Guid("{0F080147-A8DE-4DA7-9B42-4232534654E3}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = bill },
                new Calendar { Id = new Guid("{AD5DB750-BEEA-4082-A939-1E12A7F46520}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = jack },
                new Calendar { Id = new Guid("{25A825BD-9F6E-4237-BF9D-C07C751CB01C}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = sam },
                new Calendar { Id = new Guid("{3829EF28-05EE-41FF-9074-F1B458E32808}"), LastBackupDate = DateTime.Now, LastBackupStatus = "Ok", User = ann }
                );

            _context.SaveChanges();
        }

        [Test]
        public async Task GetReport_ValidParams_ReturnsOkObjectResult()
        {
            var res = await _fservice.GetReport(new GetReportRequest { DomainId = new Guid("{0B48F0DE-FC95-4B0D-B0DF-08614CA3F6A8}"), PageNumber = 1, PageSize = 1 });
            Assert.That(res, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task GetReport_NoDbData_ReturnsBadRequestObjectResult()
        {
            _context.Database.EnsureDeleted();
            var res = await _fservice.GetReport(new GetReportRequest { DomainId = new Guid("{0B48F0DE-FC95-4B0D-B0DF-08614CA3F6A8}"), PageNumber = 1, PageSize = 1 });
            Assert.That(res, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetReport_ThrowsNewException_CircuitBreaksReturnsStatus503ServiceUnavailable()
        {
            IActionResult res = null;
            _licenseServiceMock.Setup(x => x.GetLicensesAsync(It.IsAny<Guid>(), It.IsAny<ICollection<string>>())).Throws(new Exception());
            for (int i = 0; i < 3; i++)
            {
                res = await _fservice.GetReport(new GetReportRequest { DomainId = new Guid("{0B48F0DE-FC95-4B0D-B0DF-08614CA3F6A8}"), PageNumber = 1, PageSize = 1 });
            }

            var statusCodeResult = res as StatusCodeResult;
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(statusCodeResult.StatusCode, StatusCodes.Status503ServiceUnavailable);
        }
    }
}