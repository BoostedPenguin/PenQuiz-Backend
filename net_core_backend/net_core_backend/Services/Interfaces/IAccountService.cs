using net_core_backend.Models;
using System.Threading.Tasks;

namespace net_core_backend.Services.Interfaces
{
    public interface IAccountService
    {
        Task<Users> GetUserDetailsJWT(int id);
        Task<VerificationResponse> Login(LoginRequest model);
        Task<VerificationResponse> Register(AddUserRequest requestInfo);
    }
}