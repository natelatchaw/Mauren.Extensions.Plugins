using Mauren.Extensions.Plugins.Abstractions;
using Mauren.Extensions.Plugins.Internal;
using System;
using System.IO;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins
{
    /// <inheritdoc/>
    internal class PluginLoadContextFactory : IPluginLoadContextFactory
    {
        /// <inheritdoc/>
        AssemblyLoadContext IPluginLoadContextFactory.Load(FileInfo file)
        {
            // Construct a new load context for the provided assembly file
            AssemblyLoadContext pluginLoadContext = new PluginLoadContext(file.FullName, file.FullName);

            try
            {
                // Load the contents of the assembly file
                pluginLoadContext.LoadFromAssemblyPath(file.FullName);

                // Return the context
                return pluginLoadContext;
            }
            catch (Exception)
            {
                // Unload the context
                pluginLoadContext.Unload();

                // Throw exception
                throw;
            }
        }
    }
}
