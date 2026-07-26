using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// Defines a contract for discovering, loading, and managing the lifecycle
    /// of plugin implementations of <typeparamref name="TContract"/>.
    /// </summary>
    /// 
    /// <typeparam name="TContract">
    /// The plugin <see langword="interface"/> or base <see langword="class"/> 
    /// <see langword="type"/> to discover and load.
    /// </typeparam>
    public interface IPluginLoader<TContract>
    {
        /// <summary>
        /// Fired when a <typeparamref name="TContract"/> implementation has been loaded.
        /// </summary>
        event EventHandler<IPluginContext<TContract>>? PluginLoaded;

        /// <summary>
        /// Fired when a <typeparamref name="TContract"/> implementation has been unloaded.
        /// </summary>
        event EventHandler<IPluginContext<TContract>>? PluginUnloaded;

        /// <summary>
        /// Asynchronously scans the specified directory for assemblies containing
        /// <typeparamref name="TContract"/> plugin <see langword="type"/>s and initializes 
        /// them in isolated contexts.
        /// </summary>
        /// 
        /// <param name="directory">
        /// The directory to scan for assemblies implementing <typeparamref name="TContract"/>.
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
    }
}
