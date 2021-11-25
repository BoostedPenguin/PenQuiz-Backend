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
using GameService.Context;
using GameService.Services;
using GameService.Services.Interfaces;
using Microsoft.OpenApi.Models;
using GameService.Helpers;
using GameService.Hubs;
using System.Text;
using GameService.EventProcessing;
using GameService.MessageBus;
using GameService.Grpc;
using GameService.Services.GameTimerServices;

namespace GameService
{
    public class Startup
    {
        private readonly IWebHostEnvironment env;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            this.env = env;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            if (env.IsProduction())
            {
                services.AddDbContextFactory<DefaultContext>(options =>
                {
                    Console.WriteLine("--> Using production sql database");
                    options.UseSqlServer(Configuration.GetConnectionString("GamesConn"));
                });
            }
            else
            {
                services.AddDbContextFactory<DefaultContext>(options =>
                {
                    Console.WriteLine("--> Using production sql database");
                    options.UseSqlServer("Server=localhost,1433;Initial Catalog=gamesdb;User ID=sa;Password=pass00word!;");
                });
            }
            services.AddSingleton<IExampleService, ExampleService>();

            services.AddSingleton<IEventProcessor, EventProcessor>();

            services.AddSingleton<IMapGeneratorService, MapGeneratorService>();

            services.AddSingleton<IGameTerritoryService, GameTerritoryService>();

            services.AddSingleton<IGameService, Services.GameService>();

            services.AddSingleton<IGameLobbyService, GameLobbyService>();

            services.AddSingleton<IGameTimerService, GameTimerService>();

            services.AddSingleton<INeutralStageTimerEvents, NeutralStageTimerEvents>();

            services.AddSingleton<IPvpStageTimerEvents, PvpStageTimerEvents>();

            services.AddSingleton<IMessageBusClient, MessageBusClient>();

            services.AddSingleton<IGameControlService, GameControlService>();

            services.AddScoped<IAccountDataClient, AccountDataClient>();

            services.AddHttpContextAccessor();

            services.AddHttpClient();

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

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

            //services.AddHostedService<BackgroundTaskService>();

            services.AddHostedService<MessageBusSubscriber>();
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
            
            PrepDb.PrepMigration(app, env.IsProduction());
        }
    }
}
