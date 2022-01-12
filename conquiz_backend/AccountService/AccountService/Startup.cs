using AccountService.Context;
using AccountService.Data;
using AccountService.Grpc;
using AccountService.MessageBus;
using AccountService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System;
using System.IO;
using System.Text;

namespace AccountService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment environment;
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            environment = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://localhost:19006/, http://192.168.0.100:19006/, exp://192.168.0.100:19000, https://192.168.0.100.nip.io:19006/")
                                .SetIsOriginAllowed((host) => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                });
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                //options.Authority = /* TODO: Insert Authority URL here */;

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                var b = Configuration.GetSection("AppSettings").GetValue<string>("Issuer");
                var z = Configuration.GetSection("AppSettings").GetValue<string>("Secret");
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = Configuration.GetSection("AppSettings").GetValue<string>("Issuer"),
                    ValidAudience = Configuration.GetSection("AppSettings").GetValue<string>("Issuer"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings").GetValue<string>("Secret"))),
                };
            });

            services.AddSingleton<IAccountService, Services.AccountService>();

            services.AddSingleton<IMessageBusClient, MessageBusClient>();

            services.AddHttpClient();

            services.AddGrpc();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AccountService", Version = "v1" });
            });

            services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccountService v1"));
            }


            app.UseRouting();

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<GrpcAccountService>();

                endpoints.MapGet("/protos/accounts.proto", async context =>
                {
                    await context.Response.WriteAsync(File.ReadAllText("Protos/accounts.proto"));
                });
            });

            PrepDb.PrepMigration(app);

        }
    }
}
