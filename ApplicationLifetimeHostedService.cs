using System.Threading;
using System.Threading.Tasks;

//using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HerokuApp
{
    public class ApplicationLifetimeHostedService : IHostedService
    {
        IHostApplicationLifetime appLifetime;
        ILogger<ApplicationLifetimeHostedService> logger;
        IHostEnvironment environment;
        IConfiguration configuration;

        public ApplicationLifetimeHostedService(
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger<ApplicationLifetimeHostedService> logger,
            IHostApplicationLifetime appLifetime)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.appLifetime = appLifetime;
            this.environment = environment;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("StartAsync method called.");

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;

        }

        private void OnStarted()
        {
            logger.LogInformation("OnStarted method called.");

            // Post-startup code goes here  
        }

        private void OnStopping()
        {
            logger.LogInformation("OnStopping method called.");

            // On-stopping code goes here  
        }

        private void OnStopped()
        {
            logger.LogInformation("OnStopped method called.");

            // Post-stopped code goes here  
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("StopAsync method called.");

            return Task.CompletedTask;
        }
    }
}
