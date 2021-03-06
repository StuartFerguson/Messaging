using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessagingService
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using EmailMessage.DomainEvents;
    using EventStore.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Shared.EventStore.Aggregate;
    using Shared.EventStore.EventHandling;
    using Shared.EventStore.Subscriptions;
    using Shared.Logger;
    using SMSMessage.DomainEvents;

    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            Program.CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Console.Title = "Messaging Service";

            //At this stage, we only need our hosting file for ip and ports
            IConfigurationRoot config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                                  .AddJsonFile("hosting.json", optional: true)
                                                                  .AddJsonFile("hosting.development.json", optional: true)
                                                                  .AddEnvironmentVariables().Build();

            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureLogging(logging =>
                                         {
                                             logging.AddConsole();

                                         }).ConfigureWebHostDefaults(webBuilder =>
                                                                     {
                                                                         webBuilder.UseStartup<Startup>();
                                                                         webBuilder.UseConfiguration(config);
                                                                         webBuilder.UseKestrel();
                                                                     })
                       .ConfigureServices(services =>
                                                              {
                                                                  RequestSentToEmailProviderEvent e = new RequestSentToEmailProviderEvent(Guid.Parse("2AA2D43B-5E24-4327-8029-1135B20F35CE"), "", new List<String>(),
                                                                      "", "", true);

                                                                  RequestSentToSMSProviderEvent s = new RequestSentToSMSProviderEvent(Guid.NewGuid(), "", "","");
                                                                  
                                                                  TypeProvider.LoadDomainEventsTypeDynamically();

                                                                  services.AddHostedService<SubscriptionWorker>(provider =>
                                                                                                                {
                                                                                                                    IDomainEventHandlerResolver r =
                                                                                                                        provider.GetRequiredService<IDomainEventHandlerResolver>();
                                                                                                                    EventStorePersistentSubscriptionsClient p = provider.GetRequiredService<EventStorePersistentSubscriptionsClient>();
                                                                                                                    HttpClient h = provider.GetRequiredService<HttpClient>();
                                                                                                                    SubscriptionWorker worker = new SubscriptionWorker(r, p, h);
                                                                                                                    worker.TraceGenerated += Worker_TraceGenerated;
                                                                                                                    return worker;
                                                                                                                });
                                                              });
            return hostBuilder;
        }

        /// <summary>
        /// Workers the trace generated.
        /// </summary>
        /// <param name="trace">The trace.</param>
        /// <param name="logLevel">The log level.</param>
        private static void Worker_TraceGenerated(string trace, LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Logger.LogTrace(trace);
                    break;
                case LogLevel.Debug:
                    Logger.LogDebug(trace);
                    break;
                case LogLevel.Information:
                    Logger.LogInformation(trace);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(trace);
                    break;
                case LogLevel.Error:
                    Logger.LogError(new Exception(trace));
                    break;
                case LogLevel.Critical:
                    Logger.LogCritical(new Exception(trace));
                    break;
            }
        }
    }
}
