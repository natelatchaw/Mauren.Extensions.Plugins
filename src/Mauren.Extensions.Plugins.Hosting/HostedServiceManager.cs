using Mauren.Extensions.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mauren.Extensions.Plugins.Hosting
{
    /// <summary>
    /// A background service that listens for plugin load events 
    /// and automatically starts or stops <see cref="IHostedService"/> 
    /// instances provided by loaded plugin containers.
    /// </summary>
    /// 
    /// <typeparam name="TContract">
    /// The plugin contract <see langword="interface"/> or 
    /// base <see langword="class"/> <see langword="type"/>.
    /// </typeparam>
    internal class HostedServiceManager<TContract> : BackgroundService
    {
        /// <summary>
        /// The <see cref="ILogger{TCategoryName}"/> used to record operation logs.
        /// </summary>
        private readonly ILogger<HostedServiceManager<TContract>> _logger;

        /// <summary>
        /// The host application's lifetime.
        /// </summary>
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        /// <summary>
        /// The plugin loader service.
        /// </summary>
        private readonly IPluginLoader<TContract> _pluginLoader;

        /// <summary>
        /// A thread-safe registry tracking running hosted services, keyed by the 
        /// plugin context identifier.
        /// </summary>
        private readonly ConcurrentDictionary<String, IEnumerable<IHostedService>> _runningHostedServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostedServiceManager{TContract}"/>
        /// <see langword="class"/>.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The <see cref="ILogger{TCategoryName}"/> used to record service lifecycle events.
        /// </param>
        /// 
        /// <param name="pluginLoader">
        /// The plugin loader orchestrating plugin lifecycles.
        /// </param>
        /// 
        /// <param name="hostApplicationLifetime">
        /// The host application lifetime used to coordinate graceful shutdowns.
        /// </param>
        public HostedServiceManager(ILogger<HostedServiceManager<TContract>> logger, IPluginLoader<TContract> pluginLoader,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _pluginLoader = pluginLoader;
            _hostApplicationLifetime = hostApplicationLifetime;

            _runningHostedServices = new();
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Subscribe to plugin lifecycle events
            _pluginLoader.PluginLoaded += OnPluginLoaded;
            _pluginLoader.PluginUnloaded += OnPluginUnloaded;

            // Ensure all running plugin services stop gracefully when the host application stops
            using CancellationTokenRegistration registration = _hostApplicationLifetime.ApplicationStopping.Register(() =>
            {
                StopAllAsync().GetAwaiter().GetResult();
            });

            try
            {
                // Keep the background service alive indefinitely until cancellation is requested
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            // Swallow exception; expected behavior during graceful shutdown
            catch (TaskCanceledException) { }
            finally
            {
                // Unsubscribe from plugin lifecycle events
                _pluginLoader.PluginLoaded -= OnPluginLoaded;
                _pluginLoader.PluginUnloaded -= OnPluginUnloaded;
            }
        }

        private void OnPluginLoaded(object? sender, IPluginContext<TContract> e)
        {
            // Because PluginLoaded is a synchronous event, we fire-and-forget the async startup logic
            _ = RunAsync(e);
        }

        private void OnPluginUnloaded(object? sender, IPluginContext<TContract> e)
        {
            // Fire-and-forget the async teardown logic to prevent blocking the unloader
            _ = StopAsync(e.Id);
        }

        /// <summary>
        /// Resolves and starts all <see cref="IHostedService"/> instances 
        /// registered in the plugin's child container.
        /// </summary>
        /// 
        /// <param name="pluginContext">
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous run operation.
        /// </returns>
        private async Task RunAsync(IPluginContext<TContract> pluginContext)
        {
            try
            {
                // Get a collection of all hosted services from the plugin context's service provider
                List<IHostedService> hostedServices = pluginContext.Provider.GetServices<IHostedService>().ToList();

                // If no hosted services were found, return
                if (hostedServices.Count == 0) return;

                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.Log(LogLevel.Information, "Starting {count} hosted services for plugin {id}", hostedServices.Count, pluginContext.Id);

                // Iterate over the collection of hosted services
                foreach (IHostedService hostedService in hostedServices)
                {
                    try
                    {
                        // Start the hosted service and provide the host application's stopping cancellation token
                        await hostedService.StartAsync(_hostApplicationLifetime.ApplicationStopping).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                            _logger.Log(LogLevel.Error, exception, "Plugin hosted service '{serviceType}' in plugin '{pluginId}' failed to start.", hostedService.GetType(), pluginContext.Id);
                    }
                }

                // Try to add the hosted services to the registry under the plugin context's id
                _runningHostedServices.TryAdd(pluginContext.Id, hostedServices);
            }
            catch (Exception exception)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                    _logger.Log(LogLevel.Error, exception, "Could not resolve or start hosted services from plugin {pluginId}", pluginContext.Id);
            }
        }

        /// <summary>
        /// Stops all running <see cref="IHostedService"/> instances 
        /// associated with the provided plugin identifier.
        /// </summary>
        /// 
        /// <param name="pluginId">
        /// The identifier of the plugin to stop.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous stop operation.
        /// </returns>
        private async Task StopAsync(String pluginId)
        {
            // Try to remove the hosted services from the registry under the plugin context's id
            if (_runningHostedServices.TryRemove(pluginId, out IEnumerable<IHostedService>? hostedServices) is false)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.Log(LogLevel.Debug, "No hosted services were running for plugin '{pluginId}'", pluginId);

                // Return, nothing to do
                return;
            }

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.Log(LogLevel.Information, "Stopping {count} hosted services for plugin {id}", hostedServices.Count(), pluginId);

            IEnumerable<Task> stopHostedServiceTasks = hostedServices.Select(async (IHostedService hostedService) =>
            {
                try
                {
                    // Provide a bounded timeout cancellation token source for standard graceful shutdowns
                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                    // Stop the hosted service
                    await hostedService.StopAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                        _logger.Log(LogLevel.Error, exception, "Plugin hosted service '{serviceType}' in plugin '{pluginId}' failed to stop.", hostedService.GetType(), pluginId);
                }
            });

            // Wait for all hosted services to stop
            await Task.WhenAll(stopHostedServiceTasks);
        }

        /// <summary>
        /// Stops all <see cref="IHostedService"/> instances 
        /// across all loaded plugins during a host application shutdown sequence.
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous stop operation.
        /// </returns>
        private async Task StopAllAsync()
        {
            // Get a collection of all hosted service shutdown tasks
            IEnumerable<Task> stopHostedServiceTasks = _runningHostedServices.Keys.Select(StopAsync);

            // Wait for all hosted services to stop
            await Task.WhenAll(stopHostedServiceTasks).ConfigureAwait(false);
        }
    }
}