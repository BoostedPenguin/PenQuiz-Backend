using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GameService.Services.Extensions
{
    public static class CurrentExtensions
    {
        /// <summary>
        /// Gets the auth of the issuer claim
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        [Obsolete("use GetCurrentUserId", true)]
        public static string GetCurrentAuth(this IHttpContextAccessor httpContext)
        {
            // Check nameidentifier claim first -> then name claim
            var z = httpContext.HttpContext.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).ToList();
            if(z.Count != 0)
            {
                return httpContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            
            var b = httpContext.HttpContext.User.FindFirst(ClaimTypes.Name).Value;
            return b;
        }


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
