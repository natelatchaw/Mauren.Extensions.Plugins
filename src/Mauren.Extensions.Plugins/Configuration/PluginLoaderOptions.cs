using Mauren.Extensions.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Mauren.Extensions.Plugins.Configuration
{
    /// <summary>
    /// Options for configuring a contract-specific <see cref="IPluginLoader{TContract}"/>.
    /// </summary>
    public class PluginLoaderOptions
    {
        /// <summary>
        /// Gets or sets the strategy used to discover and register plugin services inside child containers.
        /// </summary>
        public IServiceRegistrationStrategy? ServiceRegistrationStrategy { get; set; }

        /// <summary>
        /// Gets or sets an optional collection of host <see cref="ServiceDescriptor"/> instances
        /// to copy or propagate into loaded plugin child containers.
        /// </summary>
        public IEnumerable<ServiceDescriptor>? HostServiceDescriptors { get; set; }
    }
}
