using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using conquiz_backend.Context;
using conquiz_backend.Services;
using conquiz_backend.Services.Interfaces;
using Microsoft.OpenApi.Models;
using conquiz_backend.Helpers;
using conquiz_backend.Hubs;
using System.Text;

namespace conquiz_backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddSingleton<IContextFactory>(new ContextFactory(Configuration.GetConnectionString("SQLCONNSTR_Database")));

            services.AddSingleton<IExampleService, ExampleService>();
            
            services.AddSingleton<IMapGeneratorService, MapGeneratorService>();

            services.AddSingleton<IQuestionService, QuestionService>();

            services.AddSingleton<IGameService, GameService>();

            services.AddSingleton<IGameLobbyService, GameLobbyService>();

            services.AddSingleton<IGameTimer, GameTimer>();

            services.AddHttpContextAccessor();

            services.AddHttpClient();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

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

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/chathubs") || path.StartsWithSegments("/gamehubs")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddSignalR().AddNewtonsoftJsonProtocol(x =>
            {
                x.PayloadSerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            
            services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddHostedService<BackgroundTaskService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }

            app.UseRouting();

            //app.UseApiResponseAndExceptionWrapper();

            app.UseCors();

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            //app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChatHub>("/chathubs");
                endpoints.MapHub<GameHub>("/gamehubs");
            });
        }
    }
}
