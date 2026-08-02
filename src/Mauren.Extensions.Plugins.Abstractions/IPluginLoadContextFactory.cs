using Microsoft.Extensions.FileProviders;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// Defines a factory for creating and initializing isolated
    /// <see cref="AssemblyLoadContext"/> instances for plugin assemblies.
    /// </summary>
    public interface IPluginLoadContextFactory
    {
        /// <summary>
        /// Creates an isolated <see cref="AssemblyLoadContext"/> and loads 
        /// the assembly represented by the specified <paramref name="fileInfo"/> instance.
        /// </summary>
        /// 
        /// <param name="fileInfo">
        /// The <see cref="IFileInfo"/> representing the target assembly file to load.
        /// </param>
        /// 
        /// <returns>
        /// An <see cref="AssemblyLoadContext"/> hosting the loaded assembly 
        /// and its isolated dependencies.
        /// </returns>
        AssemblyLoadContext Load(IFileInfo fileInfo);
    }
}
