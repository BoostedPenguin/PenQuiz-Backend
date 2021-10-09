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
using System.Security.Cryptography;

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

        public async Task<AuthenticateResponse> Authenticate(Payload payload, string ipAddress)
        {
            using(var a = contextFactory.CreateDbContext())
            {
                var user = await a.Users.Include(x => x.RefreshToken).FirstOrDefaultAsync(x => x.Email == payload.Email);

                if (user == null)
                {
                    user = new Users() { Email = payload.Email, Username = payload.Name };

                    await a.AddAsync(user);
                    await a.SaveChangesAsync();
                }

                var jwtToken = generateJwtToken(user);
                var refreshToken = generateRefreshToken(ipAddress);

                user.RefreshToken.Add(refreshToken);
                a.Update(user);
                await a.SaveChangesAsync();

                return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
            }
        }

        public async Task RevokeCookie(string token, string ipAddress)
        {
            using var a = contextFactory.CreateDbContext();
            var user = a.Users.Include(x => x.RefreshToken).SingleOrDefault(x => x.RefreshToken.Any(y => y.Token == token));

            // No user found with token
            if (user == null) return;

            var refreshToken = user.RefreshToken.Single(x => x.Token == token);
            // No active refresh tokens
            if (!refreshToken.IsActive) return;

            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = null;

            a.Update(user);
            await a.SaveChangesAsync();
        }

        public async Task<AuthenticateResponse> RefreshToken(string token, string ipaddress)
        {
            using(var a = contextFactory.CreateDbContext())
            {
                var user = a.Users.Include(x => x.RefreshToken).SingleOrDefault(x => x.RefreshToken.Any(y => y.Token == token));

                // No user found with token
                if (user == null) return null;

                var refreshToken = user.RefreshToken.Single(x => x.Token == token);
                // No active refresh tokens
                if (!refreshToken.IsActive) return null;

                var newRefreshToken = generateRefreshToken(ipaddress);
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipaddress;
                refreshToken.ReplacedByToken = newRefreshToken.Token;
                user.RefreshToken.Add(newRefreshToken);

                a.Update(user);
                await a.SaveChangesAsync();


                var jwtToken = generateJwtToken(user);

                return new AuthenticateResponse(user, jwtToken, newRefreshToken.Token);
            }
        }

        public async Task<bool> RevokeToken(string token, string ipAddress)
        {
            using (var a = contextFactory.CreateDbContext())
            {

                var user = a.Users.SingleOrDefault(u => u.RefreshToken.Any(t => t.Token == token));

                // return false if no user found with token
                if (user == null) return false;

                var refreshToken = user.RefreshToken.Single(x => x.Token == token);

                // return false if token is not active
                if (!refreshToken.IsActive) return false;

                // revoke token and save
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                a.Update(user);
                await a.SaveChangesAsync();

                return true;
            }
        }

        public async Task<Users> GetById(int id)
        {
            using (var a = contextFactory.CreateDbContext())
            {
                return await a.Users.FindAsync(id);
            }
        }

        public async Task<List<Users>> GetUsers()
        {
            using (var a = contextFactory.CreateDbContext())
            {
                return await a.Users.ToListAsync();
            }
        }

        private string generateJwtToken(Users user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = appSettings.Issuer,
                Audience = appSettings.Audience,
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[64];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return new RefreshToken
                {
                    Token = Convert.ToBase64String(randomBytes),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = ipAddress
                };
            }
        }
    }
}
