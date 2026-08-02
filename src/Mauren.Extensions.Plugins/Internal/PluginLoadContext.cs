using Microsoft.Extensions.FileProviders;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins.Internal
{
    /// <summary>
    /// Represents a custom <see cref="AssemblyLoadContext"/> for loading plugin assemblies and
    /// unmanaged DLLs.
    /// </summary>
    internal class PluginLoadContext : AssemblyLoadContext
    {
        /// <summary>
        /// An <see cref="AssemblyDependencyResolver"/> to help locate
        /// assemblies and unmanaged DLLs.
        /// </summary>
        private readonly AssemblyDependencyResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginLoadContext"/> <see langword="class"/>
        /// with the specified <paramref name="fileInfo"/> representing the <see cref="Assembly"/> file.
        ///</summary>
        ///
        /// <param name="fileInfo">
        /// An <see cref="IFileInfo"/> representing the assembly DLL. 
        /// Its physical path is used to initialize the <see cref="AssemblyDependencyResolver"/>,
        /// which helps locate assemblies and unmanaged DLLs for loading.
        /// </param>
        /// 
        /// <param name="isCollectible">
        /// <see langword="true"/> to enable <see cref="AssemblyLoadContext.Unload"/>;
        /// otherwise, <see langword="false"/>.
        /// The default value is <see langword="true"/> to allow the plugin to be unloaded.
        /// </param>
        /// 
        /// <exception cref="ArgumentException">
        /// Thrown when the provided <paramref name="fileInfo"/> does not provide a valid physical path.
        /// </exception>
        public PluginLoadContext(IFileInfo fileInfo, String? name, Boolean isCollectible = true) : base(name, isCollectible)
        {
            // If the file provider does not support physical paths
            if (String.IsNullOrEmpty(fileInfo.PhysicalPath))
                throw new ArgumentException("The provided file does not provide physical path information", nameof(fileInfo));

            // Construct a new assembly dependency resolver with the physical path to the assembly file.
            _resolver = new(fileInfo.PhysicalPath);
        }

        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Search the default Assembly Load Context for an assembly with the provided name
            Assembly? loadedAssembly = AssemblyLoadContext.Default.Assemblies
                // Filter to assemblies matching the assembly name
                .Where((Assembly assembly) => String.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase))
                // Get the only match, if it exists
                .SingleOrDefault();
            // If an assembly with an assembly name matching the provided assembly name was found
            if (loadedAssembly is Assembly existing) return existing;

            // Use the resolver to find the path to the assembly
            String? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            // If the assembly path is found
            if (assemblyPath != null)
            {
                // Load the assembly from the resolved path
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Return null to indicate that the assembly could not be loaded
            return null;
        }

        /// <inheritdoc/>
        protected override nint LoadUnmanagedDll(String unmanagedDllName)
        {
            // Use the resolver to find the path to the unmanaged DLL
            String? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            // If the library path is found
            if (libraryPath != null)
            {
                // Load the unmanaged DLL from the resolved path
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            // Return IntPtr.Zero to indicate that the unmanaged DLL could not be loaded
            return IntPtr.Zero;
        }
    }
}
