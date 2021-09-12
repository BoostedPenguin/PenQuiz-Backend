using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using net_core_backend.Helpers;
using net_core_backend.Services.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Helpers
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;

    }
}