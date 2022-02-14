using BrokenCode.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrokenCode.Repository
{
    public interface IUsersRepository
    {
        Task<IEnumerable<User>> GetBackupEnabledInDomainUsers(Guid domainId, int pageSize = 1, int pageNumber = 1);
    }

    public class UsersRepository : IUsersRepository
    {
        private readonly UserDbContext _context;

        public UsersRepository(UserDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<User>> GetBackupEnabledInDomainUsers(Guid domainId, int pageSize = 0, int pageNumber = 0)
        {
            var query = _context.Users
                    .Include(b => b.Email)
                    .Include(b => b.Drive)
                    .Include(b => b.Calendar)
                    .Where(d => d.DomainId == domainId)
                    .Where(b => b.BackupEnabled && b.State == UserState.InDomain)
                    .OrderBy(o => o.Id);

            if (pageSize > 0 && pageNumber > 0)
            {
                query.Skip(pageSize * (pageNumber - 1))
                    .Take(pageSize);
            }
            return await query.AsNoTracking()
                .ToArrayAsync();
        }
    }
}
