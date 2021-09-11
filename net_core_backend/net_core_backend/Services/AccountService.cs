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


        public async Task<Users> GetUserDetailsJWT(int id)
        {
            if (id == 0) throw new ArgumentException("There is no ID in the JWT Token");

            using (var a = contextFactory.CreateDbContext())
            {
                return await a.Users.Where(x => x.Id == id).FirstOrDefaultAsync();
            }
        }

        public async Task<VerificationResponse> Login(LoginRequest model)
        {
            using(var a = contextFactory.CreateDbContext())
            {
                var user = await a.Users.FirstOrDefaultAsync(x => x.Email == model.Email);

                if (user == null)
                {
                    throw new ArgumentException("This email isn't registered in our system");
                }

                if(!BC.Verify(model.Password, user.HashedPassword))
                {
                    throw new ArgumentException("Invalid password");
                }


                // authentication successful so generate jwt token
                var token = generateJwtToken(user);

                return new VerificationResponse(user, token);
            }
        }



        public async Task<VerificationResponse> Register(AddUserRequest requestInfo)
        {
            using (var a = contextFactory.CreateDbContext())
            {
                // Checks for existing
                if (await a.Users.FirstOrDefaultAsync(x => x.Email == requestInfo.Email) != null)
                {
                    throw new ArgumentException("There is already a user with this email in our system");
                }

                // Creates and adds a user
                // Hashes and salts password
                var user = new Users( 
                    requestInfo.Email, 
                    requestInfo.FirstName, 
                    requestInfo.LastName,
                    BC.HashPassword(requestInfo.Password));

                // Creates a JWT Token for this user
                var token = generateJwtToken(user);

                await a.AddAsync(user);
                await a.SaveChangesAsync();

                return new VerificationResponse(user, token);
            }
        }

        private string generateJwtToken(Users user)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
