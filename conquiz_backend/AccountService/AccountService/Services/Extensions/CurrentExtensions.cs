using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace AccountService.Services.Extensions
{
    public static class CurrentExtensions
    {
        /// <summary>
        /// Gets logged in user id
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="contextFactory"></param>
        /// <returns></returns>
        public static string GetCurrentUserGlobalId(this IHttpContextAccessor httpContext)
        {
            return httpContext.HttpContext.User.Claims
                .Where(x => x.Type == ClaimTypes.NameIdentifier)
                .Select(x => x.Value)
                .FirstOrDefault();
        }
    }
}
