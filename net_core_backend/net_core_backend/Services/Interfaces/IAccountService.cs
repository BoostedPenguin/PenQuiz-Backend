using net_core_backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace net_core_backend.Services.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticateResponse> Authenticate(Payload payload, string ipAddress);
        Task<Users> GetById(int id);
        Task<List<Users>> GetUsers();
        Task<AuthenticateResponse> RefreshToken(string token, string ipaddress);
        Task RevokeCookie(string token, string ipAddress);
        Task<bool> RevokeToken(string token, string ipAddress);
    }
}