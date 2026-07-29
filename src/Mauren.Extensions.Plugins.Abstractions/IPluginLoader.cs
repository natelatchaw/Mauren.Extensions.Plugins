using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// Defines a non-generic contract for scanning, loading, and managing plugin assemblies 
    /// from a file system directory.
    /// </summary>
    /// 
    /// <remarks>
    /// This non-generic abstraction allows consumers (such as web hosts or background workers) 
    /// to initiate plugin discovery without requiring direct compile-time knowledge of 
    /// the underlying contract type.
    /// </remarks>
    public interface IPluginLoader
    {
        /// <summary>
        /// Asynchronously scans the specified directory for assemblies containing valid plugin 
        /// types and initializes them in isolated contexts.
        /// </summary>
        /// 
        /// <param name="directory">
        /// The directory to scan for assemblies.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous scan and load operation.
        /// </returns>
        Task ScanAsync(DirectoryInfo directory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously scans the specified file for an assembly containing valid plugin
        /// types and initializes it in an isolated context.
        /// </summary>
        /// 
        /// <param name="fileInfo">
        /// The file to scan for an assembly.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous scan and load operation,
        /// containing the <see cref="String"/> identifier of the loaded context.
        /// </returns>
        /// 
        /// <exception cref="Exception">
        /// Thrown if the load operation failed for the provided <paramref name="fileInfo"/>.
        /// </exception>
        Task<String> LoadAsync(FileInfo fileInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously unloads the isolated plugin context specified by its identifier.
        /// </summary>
        /// 
        /// <param name="id">
        /// The identifier of the loaded plugin context.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="Task"/> that represents the asynchronous unload operation.
        /// </returns>
        /// 
        /// <exception cref="Exception">
        /// Thrown if the unload operation failed for the provided <paramref name="id"/>.
        /// </exception>
        Task UnloadAsync(String id, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Defines a contract for discovering, loading, and managing the lifecycle
    /// of plugin implementations of <typeparamref name="TContract"/>.
    /// </summary>
    /// 
    /// <typeparam name="TContract">
    /// The plugin <see langword="interface"/> or base <see langword="class"/> 
    /// <see langword="type"/> to discover and load.
    /// </typeparam>
    public interface IPluginLoader<TContract> : IPluginLoader
    {
        /// <summary>
        /// Fired when a <typeparamref name="TContract"/> implementation has been loaded.
        /// </summary>
        event EventHandler<IPluginContext<TContract>>? PluginLoaded;

        /// <summary>
        /// Fired when a <typeparamref name="TContract"/> implementation has been unloaded.
        /// </summary>
        event EventHandler<IPluginContext<TContract>>? PluginUnloaded;
    }
}
