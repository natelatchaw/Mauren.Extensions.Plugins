using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// Defines a factory for constructing an isolated <see cref="IServiceProvider"/>
    /// tailored to an individual plugin's execution context.
    /// </summary>
    public interface IPluginContainerFactory
    {
        /// <summary>
        /// Builds a dependency injection service provider for a plugin, integrating 
        /// discovered plugin services and optional host application dependencies.
        /// </summary>
        /// 
        /// <param name="assemblyLoadContext">
        /// The <see cref="AssemblyLoadContext"/> containing the plugin assembly and
        /// its isolated dependencies.
        /// </param>
        /// 
        /// <param name="types">
        /// The collection of discovered plugin types within the loaded assembly to be
        /// registered with the container.
        /// </param>
        /// 
        /// <param name="serviceRegistrationStrategy">
        /// The strategy used to discover and apply custom service registrations from 
        /// within the plugin assembly.
        /// </param>
        /// 
        /// <param name="serviceProvider">
        /// The host application's <see cref="IServiceProvider"/> used to resolve
        /// host-level services or singletons.
        /// </param>
        /// 
        /// <param name="serviceDescriptors">
        /// An optional collection of host <see cref="ServiceDescriptor"/> instances
        /// to propagate or mirror in the child container.
        /// </param>
        /// 
        /// <returns>
        /// An <see cref="IServiceProvider"/> scoped specifically to the provided
        /// plugin context.
        /// </returns>
        IServiceProvider Create(AssemblyLoadContext assemblyLoadContext, IEnumerable<Type> types,
            IServiceRegistrationStrategy serviceRegistrationStrategy, IServiceProvider serviceProvider, 
            IEnumerable<ServiceDescriptor>? serviceDescriptors);
    }
}
