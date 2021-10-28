using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GameService.Services;
using GameService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameService
{
    public class BackgroundTaskService : IHostedService
    {
        private readonly IServiceProvider serviceProvider;

        public BackgroundTaskService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();

            // Validate map
            var mapGeneratorService = scope.ServiceProvider.GetRequiredService<IMapGeneratorService>();

            await mapGeneratorService.ValidateMap();

            // Cancel all "stuck" games
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            await gameService.CancelOngoingGames();

            // Validate questions
            var questionService = scope.ServiceProvider.GetRequiredService<IQuestionService>();

            await questionService.AddDefaultQuestions();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
