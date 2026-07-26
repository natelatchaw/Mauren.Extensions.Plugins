using System;
using System.Collections.Generic;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// Defines a service for inspecting loaded assemblies within 
    /// an <see cref="AssemblyLoadContext"/> to discover 
    /// <see langword="type"/>s implementing <typeparamref name="TContract"/>.
    /// </summary>
    /// 
    /// <typeparam name="TContract">
    /// The plugin contract <see langword="interface"/> or base 
    /// <see langword="class"/> <see langword="type"/> to discover.
    /// </typeparam>
    public interface IPluginTypeDiscoverer<TContract>
    {
        /// <summary>
        /// Discovers <see langword="type"/>s within the specified 
        /// <see cref="AssemblyLoadContext"/> that implement, derive from,
        /// or satisfy the criteria for <typeparamref name="TContract"/>.
        ///</summary>
        /// 
        /// <param name="assemblyLoadContext">
        /// The <see cref="AssemblyLoadContext"/> containing the assemblies to inspect.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="IReadOnlyCollection{T}"/> of discovered <see cref="Type"/>s 
        /// implementing <typeparamref name="TContract"/>.
        /// </returns>
        IReadOnlyCollection<Type> Discover(AssemblyLoadContext assemblyLoadContext);
    }
}
