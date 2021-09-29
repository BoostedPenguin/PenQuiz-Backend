using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using net_core_backend.Services;
using net_core_backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace net_core_backend
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
            var service = scope.ServiceProvider.GetRequiredService<IMapGeneratorService>();

            await service.ValidateMap();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
