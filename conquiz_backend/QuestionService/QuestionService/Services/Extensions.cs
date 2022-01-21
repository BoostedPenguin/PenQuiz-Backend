using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace QuestionService.Services
{
    public static class Extensions
    {
        /// <summary>
        /// Gets logged in user id
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="contextFactory"></param>
        /// <returns></returns>
        public static int GetCurrentUserId(this IHttpContextAccessor httpContext)
        {
            return int.Parse(httpContext.HttpContext.User.Claims
                .Where(x => x.Type == ClaimTypes.NameIdentifier)
                .Select(x => x.Value)
                .FirstOrDefault());
        }

        /// <summary>
        /// Gets logged in user id
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="contextFactory"></param>
        /// <returns></returns>
        public static string GetCurrentUserRole(this IHttpContextAccessor httpContext)
        {
            return httpContext.HttpContext.User.Claims
                .Where(x => x.Type == ClaimTypes.Role)
                .Select(x => x.Value)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets logged in user id
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="contextFactory"></param>
        /// <returns></returns>
        public static string GetCurrentUserName(this IHttpContextAccessor httpContext)
        {
            return httpContext.HttpContext.User.Claims
                .Where(x => x.Type == ClaimTypes.Name)
                .Select(x => x.Value)
                .FirstOrDefault();
        }

        public static string FirstCharToUpper(string s)
        {
            // Check for empty string.  
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.  
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
