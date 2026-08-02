using Mauren.Extensions.Plugins;
using Mauren.Extensions.Plugins.Abstractions;
using Mauren.Extensions.Plugins.Configuration;
using Mauren.Extensions.Plugins.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up plugin services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers core, non-generic plugin loader infrastructure services 
        /// (load context factory and child container factory) if they have not already been registered.
        /// </summary>
        /// 
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add services to.
        /// </param>
        /// 
        /// <returns>
        /// The <see cref="IServiceCollection"/> for chaining.
        /// </returns>
        public static IServiceCollection AddPluginCore(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Try to add the plugin load context factory as a singleton service
            services.TryAddSingleton<IPluginLoadContextFactory, PluginLoadContextFactory>();

            // Try to add the plugin container factory as a singleton service
            services.TryAddSingleton<IPluginContainerFactory, PluginContainerFactory>();

            // Return the service collection for chaining
            return services;
        }

        /// <summary>
        /// Registers an isolated <see cref="IPluginLoader{TContract}"/> and its <typeparamref name="TContract"/>
        /// <see langword="type"/> discoverer.
        /// </summary>
        /// 
        /// <typeparam name="TContract">
        /// The plugin contract <see langword="interface"/> or base <see langword="class"/> <see langword="type"/>.
        /// </typeparam>
        /// 
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add services to.
        /// </param>
        /// 
        /// <param name="configureOptions">
        /// An optional <see langword="delegate"/> to configure custom loader options or registration strategies.
        /// </param>
        /// 
        /// <returns>
        /// The <see cref="IServiceCollection"/> for chaining.
        /// </returns>
        public static IServiceCollection AddPluginLoader<TContract>(this IServiceCollection services, IConfigurationManager configuration, Action<PluginLoaderOptions> configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Ensure core infrastructure is registered
            services.AddPluginCore();

            // Construct a plugin loader options instance
            PluginLoaderOptions options = new();
            // Apply the caller's action to the plugin loader options instance
            configureOptions?.Invoke(options);

            // Try to add the plugin type discoverer as a singleton service
            services.TryAddSingleton<IPluginTypeDiscoverer<TContract>, PluginTypeDiscoverer<TContract>>();

            // Try to add the plugin loader as a singleton service
            services.TryAddSingleton<IPluginLoader<TContract>>((IServiceProvider serviceProvider) =>
            {
                ILogger<PluginLoader<TContract>> logger = serviceProvider.GetService<ILogger<PluginLoader<TContract>>>() switch
                {
                    ILogger<PluginLoader<TContract>> value => value,
                    _ => new NullLogger<PluginLoader<TContract>>(),
                };

                IPluginLoadContextFactory pluginLoadContextFactory = serviceProvider.GetRequiredService<IPluginLoadContextFactory>();
                IPluginContainerFactory pluginContainerFactory = serviceProvider.GetRequiredService<IPluginContainerFactory>();
                IPluginTypeDiscoverer<TContract> pluginTypeDiscoverer = serviceProvider.GetRequiredService<IPluginTypeDiscoverer<TContract>>();

                // Get a reference to the host application's service provider
                IServiceProvider hostProvider = serviceProvider;
                // Get the host application's service descriptors if provided via options (or an empty collection)
                IEnumerable<ServiceDescriptor>? hostServiceDescriptors = options.HostServiceDescriptors ?? [];

                // Get the service registration strategy if provided via options (or the default strategy)
                IServiceRegistrationStrategy? serviceRegistrationStrategy = options.ServiceRegistrationStrategy ?? new StartupClass(configuration);
                
                // Construct a plugin loader instance and return it
                return new PluginLoader<TContract>(logger, pluginLoadContextFactory, pluginTypeDiscoverer, pluginContainerFactory, serviceRegistrationStrategy, hostProvider, hostServiceDescriptors);
            });
            // Try to add the non-generic plugin loader as a singleton service
            services.TryAddSingleton<IPluginLoader>((IServiceProvider serviceProvider) =>
            {
                return serviceProvider.GetRequiredService<IPluginLoader<TContract>>();
            });
            // Return the service collection for chaining
            return services;
        }
    }
}
