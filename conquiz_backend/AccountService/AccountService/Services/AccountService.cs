using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Google.Apis.Auth.GoogleJsonWebSignature;
using System.Security.Cryptography;
using AccountService.Context;
using AccountService.MessageBus;
using AutoMapper;
using AccountService.Dtos;
using AccountService.Data.Models;
using AccountService.Data.Models.Requests;
using AccountService.Data;
using Microsoft.Extensions.Hosting;

namespace AccountService.Services
{
    public interface IAccountService
    {
        Task<AuthenticateResponse> Authenticate(Payload payload, string ipAddress);
        Task<List<Users>> GetUsers();
        string IssueDebugJwtToken(string role);
        Task<AuthenticateResponse> RefreshToken(string token, string ipaddress);
        Task<bool> RevokeCookie(string token, string ipAddress);
        Task<bool> RevokeToken(string token, string ipAddress);
    }

    public class AccountService : IAccountService
    {
        private readonly IDbContextFactory<AppDbContext> contextFactory;
        private readonly IMessageBusClient messageBusClient;
        private readonly IMapper mapper;
        private readonly AppSettings appSettings;
        public AccountService(IDbContextFactory<AppDbContext> _contextFactory, IOptions<AppSettings> appSettings, IMessageBusClient messageBusClient, IMapper mapper)
        {
            contextFactory = _contextFactory;
            this.messageBusClient = messageBusClient;
            this.mapper = mapper;
            this.appSettings = appSettings.Value;
        }

        public async Task<AuthenticateResponse> Authenticate(Payload payload, string ipAddress)
        {
            using var a = contextFactory.CreateDbContext();

            var user = await a.Users.Include(x => x.RefreshToken).FirstOrDefaultAsync(x => x.Email == payload.Email);

            if (user == null)
            {
                // Activate only in development
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development && payload.Email == "legendsxchaos@gmail.com")
                {
                    user = new Users() { Email = payload.Email, Username = payload.Name, UserGlobalIdentifier = Guid.NewGuid().ToString(), Role = "admin" };
                }
                else
                {
                    user = new Users() { Email = payload.Email, Username = payload.Name, UserGlobalIdentifier = Guid.NewGuid().ToString(), Role = "user" };
                }


                await a.AddAsync(user);
                await a.SaveChangesAsync();

                try
                {
                    var userPublishedDto = mapper.Map<UserCreatedDto>(user);
                    userPublishedDto.Event = "User_Published";
                    messageBusClient.PublishNewUser(userPublishedDto);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Could not send DTO to bus: {ex.Message}");
                }
            }

            var jwtToken = generateJwtToken(user);
            var refreshToken = generateRefreshToken(ipAddress);

            // On login, remove all active refresh tokens except the new one 
            var activeRF = await a.RefreshToken.Where(x => x.UsersId == user.Id && x.Revoked == null).ToListAsync();
            activeRF.ForEach(x =>
            {
                x.Revoked = DateTime.UtcNow;
                x.RevokedByIp = ipAddress;
                x.ReplacedByToken = refreshToken.Token;
            });

            user.RefreshToken.Add(refreshToken);
            a.Update(user);
            await a.SaveChangesAsync();

            return new AuthenticateResponse(user, jwtToken, refreshToken.Token);
        }

        public async Task<bool> RevokeCookie(string token, string ipAddress)
        {
            using var a = contextFactory.CreateDbContext();
            var user = a.Users.Include(x => x.RefreshToken).SingleOrDefault(x => x.RefreshToken.Any(y => y.Token == token));

            // No user found with token
            if (user == null) return false;

            var refreshToken = user.RefreshToken.Single(x => x.Token == token);
            // No active refresh tokens
            if (!refreshToken.IsActive) return false;

            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = null;

            a.Update(user);
            await a.SaveChangesAsync();

            return true;
        }

        public async Task<AuthenticateResponse> RefreshToken(string token, string ipaddress)
        {
            using var a = contextFactory.CreateDbContext();

            var rToken = await a.RefreshToken.Include(x => x.Users).FirstOrDefaultAsync(x => x.Token == token);
            if (rToken == null || !rToken.IsActive) return null;

            var newRefreshToken = generateRefreshToken(ipaddress);
            rToken.Revoked = DateTime.UtcNow;
            rToken.RevokedByIp = ipaddress;
            rToken.ReplacedByToken = newRefreshToken.Token;

            rToken.Users.RefreshToken.Add(newRefreshToken);

            a.Update(rToken.Users);
            await a.SaveChangesAsync();


            var jwtToken = generateJwtToken(rToken.Users);

            return new AuthenticateResponse(rToken.Users, jwtToken, newRefreshToken.Token);
        }

        public async Task<bool> RevokeToken(string token, string ipAddress)
        {
            using var a = contextFactory.CreateDbContext();

            var rfToken = await a.RefreshToken.FirstOrDefaultAsync(x => x.Token == token);

            // return false if no user found with token
            if (rfToken == null) return false;

            // return false if token is not active
            if (!rfToken.IsActive) return false;

            // revoke token and save
            rfToken.Revoked = DateTime.UtcNow;
            rfToken.RevokedByIp = ipAddress;
            a.Update(rfToken);
            await a.SaveChangesAsync();

            return true;
        }

        public async Task<List<Users>> GetUsers()
        {
            using var a = contextFactory.CreateDbContext();
            return await a.Users.ToListAsync();
        }
        Random r = new Random();

        public string IssueDebugJwtToken(string role)
        {
            return generateJwtToken(new Users()
            {
                Username = $"TestingUser - {r.Next(0, 10000)}",
                Role = role == "admin" ? "admin" : role == "user" ? "user" : throw new ArgumentException($"Invalid role ${role}"),
                Id = r.Next(0, 10000)
            });
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
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Name, user.Username),
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
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
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
