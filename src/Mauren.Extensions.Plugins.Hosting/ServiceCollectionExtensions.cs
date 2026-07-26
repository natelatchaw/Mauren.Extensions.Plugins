using Mauren.Extensions.Plugins.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up plugin hosting services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a background service that automatically manages the lifecycle of <see cref="IHostedService"/> 
        /// instances discovered within loaded <typeparamref name="TContract"/> plugins.
        /// </summary>
        /// 
        /// <typeparam name="TContract">
        /// The plugin contract <see langword="interface"/> or <see langword="base"/> <see langword="class"/> type.
        /// </typeparam>
        /// 
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add services to.
        /// </param>
        /// 
        /// <returns>
        /// The <see cref="IServiceCollection"/> for chaining.
        /// </returns>
        public static IServiceCollection AddHostedServiceManager<TContract>(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            // Register the manager as a standard generic host background service
            services.AddHostedService<HostedServiceManager<TContract>>();

            // Return the service collection for chaining
            return services;
        }
    }
}
