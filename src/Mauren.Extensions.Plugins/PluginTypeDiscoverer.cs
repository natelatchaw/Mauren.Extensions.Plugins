using Mauren.Extensions.Plugins.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins
{
    internal class PluginTypeDiscoverer<TContract> : IPluginTypeDiscoverer<TContract>
    {
        /// <inheritdoc/>
        IReadOnlyCollection<Type> IPluginTypeDiscoverer<TContract>.Discover(AssemblyLoadContext assemblyLoadContext)
        {
            // Get a list of applicable types from the assembly load context
            List<Type> discovered = assemblyLoadContext.Assemblies
                // Select exported types from each assembly
                .SelectMany((Assembly assembly) => assembly.GetExportedTypes())
                // Filter to types assignable to the generic type parameter
                .Where((Type type) => typeof(TContract).IsAssignableFrom(type))
                // Filter to non-abstract classes
                .Where((Type type) => type is { IsClass: true, IsAbstract: false })
                // Create a list from results
                .ToList();

            // If no matching types were discovered
            if (discovered.Count == 0)
                throw new InvalidOperationException($"No concrete types assignable to '{typeof(TContract).Name}' were discovered in '{assemblyLoadContext.Name}'");

            // Return the list of applicable types
            return discovered;
        }
    }
}
