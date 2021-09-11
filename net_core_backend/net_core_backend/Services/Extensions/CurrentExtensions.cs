using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using net_core_backend.Context;
using net_core_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace net_core_backend.Services.Extensions
{
    public static class CurrentExtensions
    {
        /// <summary>
        /// Gets the auth of the issuer claim
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
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
        public static int GetCurrentUserId(this IHttpContextAccessor httpContext)
        {
            return ((Users)httpContext.HttpContext.Items["User"]).Id;
        }
    }
}
