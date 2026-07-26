using Mauren.Extensions.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins
{
    /// <inheritdoc/>
    internal class PluginContainerFactory : IPluginContainerFactory
    {
        /// <inheritdoc/>
        IServiceProvider IPluginContainerFactory.Create(AssemblyLoadContext assemblyLoadContext, IEnumerable<Type> types,
            IServiceRegistrationStrategy serviceRegistrationStrategy, IServiceProvider serviceProvider, 
            IEnumerable<ServiceDescriptor>? serviceDescriptors)
        {
            // Construct a new service collection instance
            IServiceCollection services = new ServiceCollection();

            // Get a collection of exported types from the assembly load context's assemblies collection
            // that have been determined to be capable of registering services
            IEnumerable<Type> exportedTypes = assemblyLoadContext.Assemblies
                // Select exported types from each assembly
                .SelectMany((Assembly assembly) => assembly.GetExportedTypes())
                // Filter to types matching the service registration strategy's predicate
                .Where(serviceRegistrationStrategy.ProvidesServices.Invoke);

            // Iterate over the collection of exported types
            foreach (Type exportedType in exportedTypes)
            {
                // Apply the type's registration logic to the service collection instance
                serviceRegistrationStrategy.Apply(exportedType, services);
            }

            // Iterate over the collection of discovered types
            foreach (Type type in types)
            {
                // Add the discovered type as a transient service
                services.AddTransient(type);
            }

            // If the host application's service descriptor collection was provided
            if (serviceDescriptors != null)
            {
                CopyHostServices(services, serviceProvider, serviceDescriptors);
            }

            // Return the built service provider
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Copies or delegates <see cref="ServiceDescriptor"/>s from the host application 
        /// into the plugin's <see cref="IServiceCollection"/>, preserving singleton instances 
        /// where available.
        /// </summary>
        /// 
        /// <param name="services">
        /// The target <see cref="IServiceCollection"/> for the plugin context.
        /// </param>
        /// 
        /// <param name="serviceProvider">
        /// The host application's <see cref="IServiceProvider"/> used to resolve active 
        /// singleton instances.
        /// </param>
        /// 
        /// <param name="serviceDescriptors">
        /// The host application's collection of <see cref="ServiceDescriptor"/>s to propagate.
        /// </param>
        private static void CopyHostServices(IServiceCollection services, IServiceProvider serviceProvider, IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            // Iterate over each service descriptor in the collection
            foreach (ServiceDescriptor serviceDescriptor in serviceDescriptors)
            {
                // If the service descriptor's lifetime is singleton
                if (serviceDescriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    try
                    {
                        // Get an instance of the host application's service matching the service descriptor
                        Object? instance = serviceProvider.GetService(serviceDescriptor.ServiceType);

                        // If the instance of the host application's service is not null
                        if (instance is not null)
                        {
                            // Add the host application's service to the provided service collection as a singleton service
                            services.AddSingleton(serviceDescriptor.ServiceType, instance);

                            // Continue to the next service descriptor
                            continue;
                        }
                    }
                    // Swallow exceptions
                    catch { }
                }

                // Add the host application's service descriptor to the provided service collection
                services.Add(serviceDescriptor);
            }
        }
    }
}
