using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using net_core_backend.Context;
using net_core_backend.Helpers;
using net_core_backend.Models;
using net_core_backend.Services.Interfaces;
using net_core_backend.Services.Extensions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace net_core_backend.Services
{
    public class AccountService : DataService<DefaultModel>, IAccountService
    {
        private readonly IContextFactory contextFactory;
        private readonly IHttpContextAccessor httpContext;
        private readonly AppSettings appSettings;
        public AccountService(IContextFactory _contextFactory, IOptions<AppSettings> appSettings, IHttpContextAccessor httpContext) : base(_contextFactory)
        {
            contextFactory = _contextFactory;
            this.httpContext = httpContext;
            this.appSettings = appSettings.Value;
        }
        //TODO
        public async Task<Users> Authenticate(Payload payload)
        {
            using(var a = contextFactory.CreateDbContext())
            {
                var user = await a.Users.FirstOrDefaultAsync(x => x.Email == payload.Email);

                if (user == null)
                {
                    user = new Users(
                        payload.Email,
                        payload.Name,
                        payload.Subject,
                        payload.Issuer);

                    await a.AddAsync(user);
                    await a.SaveChangesAsync();
                }

                return user;
            }
        }



        public async Task<bool> Register(Payload payload)
        {
            using (var a = contextFactory.CreateDbContext())
            {
                // Checks for existing
                if (await a.Users.FirstOrDefaultAsync(x => x.Email == payload.Email) != null)
                {
                    throw new ArgumentException("There is already a user with this email in our system");
                }

                // Creates and adds a user
                // Hashes and salts password
                var user = new Users(
                    payload.Email,
                    payload.Name, 
                    payload.Subject,
                    payload.Issuer);

                await a.AddAsync(user);
                await a.SaveChangesAsync();

                return true;
            }
        }
    }
}
