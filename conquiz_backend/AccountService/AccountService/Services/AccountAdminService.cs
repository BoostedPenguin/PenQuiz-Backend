using AccountService.Data;
using AccountService.Data.Models.Requests;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Services
{
    public interface IAccountAdminService
    {
        Task BanAccount(BanAccountRequest request);
        Task<PaginatedAccountsResponse> GetAccounts(int pageNumber, int pageEntries);
    }
    public class AccountAdminService : IAccountAdminService
    {
        private readonly IDbContextFactory<AppDbContext> contextFactory;

        public AccountAdminService(IDbContextFactory<AppDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public async Task<PaginatedAccountsResponse> GetAccounts(int pageNumber, int pageEntries)
        {
            using var db = contextFactory.CreateDbContext();
            var baseQuery = db.Users
                .OrderByDescending(x => x.CreatedAt)
                .AsNoTracking();

            var accounts = await baseQuery
                .Skip((pageNumber - 1) * pageEntries)
                .Take(pageEntries)
                .ToListAsync();

            var count = baseQuery.Count();

            return new PaginatedAccountsResponse()
            {
                Users = accounts,
                PageIndex = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageEntries),
            };
        }

        public async Task BanAccount(BanAccountRequest request)
        {
            using var db = contextFactory.CreateDbContext();

            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.AccountId);

            if (user == null)
                throw new ArgumentException("User with this ID doesn't exist");

            if (user.IsBanned)
                throw new ArgumentException($"User with ID: {request.AccountId}, has already been banned");

            user.IsBanned = true;
            db.Update(user);

            await db.SaveChangesAsync();
        }
    }
}
