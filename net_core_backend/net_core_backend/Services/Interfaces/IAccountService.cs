using net_core_backend.Models;
using System.Threading.Tasks;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace net_core_backend.Services.Interfaces
{
    public interface IAccountService
    {
        Task<Users> Authenticate(Payload payload);
    }
}