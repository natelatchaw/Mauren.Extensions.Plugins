using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// Represents an isolated execution context and metadata for a loaded plugin <see cref="Assembly"/> 
    /// containing <typeparamref name="TContract"/> implementations.
    /// </summary>
    /// 
    /// <typeparam name="TContract">
    /// The plugin <see langword="interface"/> or base <see langword="class"/> 
    /// <see langword="type"/> associated with the context.
    /// </typeparam>
    public interface IPluginContext<TContract>
    {
        /// <summary>
        /// Gets the unique identifier for the plugin context or <see cref="Assembly"/>.
        /// </summary>
        String Id { get; }

        /// <summary>
        /// Gets the collection of discovered <see cref="Type"/>s associated with 
        /// <typeparamref name="TContract"/> within the loaded assembly.
        /// </summary>
        IEnumerable<Type> Types { get; }

        /// <summary>
        /// Gets the isolated <see cref="AssemblyLoadContext"/> hosting the plugin
        /// <see cref="Assembly"/> and its dependencies.
        /// </summary>
        AssemblyLoadContext Context { get; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> containing services and dependencies 
        /// scoped to this plugin context.
        /// </summary>
        IServiceProvider Provider { get; }
    }
}
