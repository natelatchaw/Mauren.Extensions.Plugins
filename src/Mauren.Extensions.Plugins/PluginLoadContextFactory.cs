using Mauren.Extensions.Plugins.Abstractions;
using Mauren.Extensions.Plugins.Internal;
using Microsoft.Extensions.FileProviders;
using System;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins
{
    /// <inheritdoc/>
    internal class PluginLoadContextFactory : IPluginLoadContextFactory
    {
        /// <inheritdoc/>
        AssemblyLoadContext IPluginLoadContextFactory.Load(IFileInfo fileInfo)
        {
            // If the file provider does not support physical paths
            if (String.IsNullOrEmpty(fileInfo.PhysicalPath))
                throw new ArgumentException("The provided file does not provide physical path information", nameof(fileInfo));

            // Construct a new load context for the provided assembly file
            AssemblyLoadContext pluginLoadContext = new PluginLoadContext(fileInfo, fileInfo.Name);

            try
            {
                // Load the contents of the assembly file
                pluginLoadContext.LoadFromAssemblyPath(fileInfo.PhysicalPath);

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
