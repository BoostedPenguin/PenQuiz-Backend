using System;
using System.Threading.Tasks;
using GameService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using GameService.Context;
using GameService.EventProcessing;
using GameService.Grpc;
using GameService.Helpers;
using GameService.Hubs;
using GameService.MessageBus;
using GameService.Services;
using GameService.Services.GameTimerServices;
using GameService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameService.Services.GameTimerServices.NeutralTimerServices;
using GameService.Services.GameTimerServices.PvpTimerServices;
using GameService.Services.GameUserActions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var provider = builder.Configuration.GetValue("Provider", "SqlServer");


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


if (builder.Environment.IsProduction())
{
    Console.WriteLine($"--> Using production database with provider: {provider}");


    builder.Services.AddDbContextFactory<DefaultContext>(
        options => _ = provider switch
        {
            "Npgsql" => options.UseNpgsql(builder.Configuration.GetConnectionString("GamesConnNpgsql"),
        x => x.MigrationsAssembly("GameService.NpgsqlMigrations")),

            "SqlServer" => options.UseSqlServer(
                builder.Configuration.GetConnectionString("GamesConn"),
                x => x.MigrationsAssembly("GameService.SqlServerMigrations")),

            _ => throw new Exception($"Unsupported provider: {provider}")
        });
}
else
{
    Console.WriteLine($"--> Using development database with provider: {provider}");

    builder.Services.AddDbContextFactory<DefaultContext>(
        options => _ = provider switch
        {
            "Npgsql" => options.UseNpgsql(builder.Configuration.GetConnectionString("GamesConnDevNpgsql"),
        x => x.MigrationsAssembly("GameService.NpgsqlMigrations")),

            "SqlServer" => options.UseInMemoryDatabase("InMem"),

            _ => throw new Exception($"Unsupported provider: {provider}")
        });
}


builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddSingleton<IExampleService, ExampleService>();

builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

builder.Services.AddSingleton<IMapGeneratorService, MapGeneratorService>();

builder.Services.AddSingleton<IGameTerritoryService, GameTerritoryService>();

builder.Services.AddSingleton<IGameService, GameService.Services.GameService>();

builder.Services.AddSingleton<IGameLobbyService, GameLobbyService>();

builder.Services.AddSingleton<IGameTimerService, GameTimerService>();

// Neutral stage
builder.Services.AddSingleton<INeutralNumberTimerEvents, NeutralNumberTimerEvents>();

builder.Services.AddSingleton<INeutralMCTimerEvents, NeutralMCTimerEvents>();

// Pvp stage
builder.Services.AddSingleton<IPvpStageTimerEvents, PvpStageTimerEvents>();

builder.Services.AddSingleton<IFinalPvpQuestionService, FinalPvpQuestionService>();

// Capital stage events
builder.Services.AddSingleton<ICapitalStageTimerEvents, CapitalStageTimerEvents>();

builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

// Normal game user action

builder.Services.AddSingleton<IGameControlService, GameControlService>();

builder.Services.AddSingleton<IAnswerQuestionService, AnswerQuestionService>();

builder.Services.AddSingleton<ITerritorySelectionService, TerritorySelectionService>();

builder.Services.AddScoped<IAccountDataClient, AccountDataClient>();


// Helper stage services

builder.Services.AddSingleton<ICurrentStageQuestionService, CurrentStageQuestionService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddCors(options =>
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

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration.GetSection("AppSettings").GetValue<string>("Issuer"),
        ValidAudience = builder.Configuration.GetSection("AppSettings").GetValue<string>("Issuer"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetSection("AppSettings").GetValue<string>("Secret"))),
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

builder.Services.AddSignalR();

builder.Services.AddControllers();

//services.AddHostedService<BackgroundTaskService>();

builder.Services.AddHostedService<MessageBusSubscriber>();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
app.UseRouting();

app.UseCors();

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Production)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/chathubs");
app.MapHub<GameHub>("/gamehubs");

PrepDb.PrepMigration(app, builder.Environment.IsProduction());

app.Run();
