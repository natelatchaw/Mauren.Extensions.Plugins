using Mauren.Extensions.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace Mauren.Extensions.Plugins
{
    internal class PluginLoader<TContract> : IPluginLoader<TContract>
    {
        /// <summary>
        /// The <see cref="ILogger{TCategoryName}"/> used to record operation logs.
        /// </summary>
        private readonly ILogger<PluginLoader<TContract>> _logger;

        /// <summary>
        /// The factory responsible for creating isolated <see cref="AssemblyLoadContext"/>
        /// instances.
        /// </summary>
        private readonly IPluginLoadContextFactory _pluginLoadContextFactory;

        /// <summary>
        /// The service responsible for discovering <see langword="type"/>s matching 
        /// <typeparamref name="TContract"/> within loaded assemblies.
        /// </summary>
        private readonly IPluginTypeDiscoverer<TContract> _pluginTypeDiscoverer;

        /// <summary>
        /// The factory responsible for constructing isolated child <see cref="IServiceProvider"/>
        /// instances for loaded plugins.
        /// </summary>
        private readonly IPluginContainerFactory _pluginContainerFactory;

        /// <summary>
        /// The strategy used to discover and apply custom service registrations from plugin assemblies.
        /// </summary>
        private readonly IServiceRegistrationStrategy _serviceRegistrationStrategy;

        /// <summary>
        /// The host application's <see cref="IServiceProvider"/>, containing
        /// services registered via the host dependency injection container.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// A collection of the host application's <see cref="ServiceDescriptor"/>s.
        /// </summary>
        private readonly IEnumerable<ServiceDescriptor> _serviceDescriptors;

        /// <summary>
        /// The active registry storing loaded plugin contexts mapped by their unique identifiers.
        /// </summary>
        private readonly ConcurrentDictionary<String, IPluginContext<TContract>> _registry = new();

        /// <inheritdoc/>
        public event EventHandler<IPluginContext<TContract>>? PluginLoaded;
        /// <inheritdoc/>
        public event EventHandler<IPluginContext<TContract>>? PluginUnloaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginLoader{TContract}"/> class.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The logger used to record plugin loader operations.
        /// </param>
        /// 
        /// <param name="pluginLoadContextFactory">
        /// The factory used to create isolated assembly load contexts.
        /// </param>
        /// <param name="pluginTypeDiscoverer">
        /// The service used to discover matching plugin <see langword="type"/>s in loaded assemblies.
        /// </param>
        /// 
        /// <param name="pluginContainerFactory">
        /// The factory used to build child service providers for loaded plugins.
        /// </param>
        /// 
        /// <param name="serviceRegistrationStrategy">
        /// The strategy used to register custom plugin services.
        /// </param>
        /// 
        /// <param name="serviceProvider">
        /// The host application's primary service provider.
        /// </param>
        /// 
        /// <param name="serviceDescriptors">
        /// The collection of host service descriptors to propagate.
        /// </param>
        public PluginLoader(
            ILogger<PluginLoader<TContract>> logger,
            IPluginLoadContextFactory pluginLoadContextFactory,
            IPluginTypeDiscoverer<TContract> pluginTypeDiscoverer,
            IPluginContainerFactory pluginContainerFactory,
            IServiceRegistrationStrategy serviceRegistrationStrategy,
            IServiceProvider serviceProvider,
            IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            _logger = logger;
            _pluginLoadContextFactory = pluginLoadContextFactory;
            _pluginTypeDiscoverer = pluginTypeDiscoverer;
            _pluginContainerFactory = pluginContainerFactory;
            _serviceRegistrationStrategy = serviceRegistrationStrategy;
            _serviceProvider = serviceProvider;
            _serviceDescriptors = serviceDescriptors;
        }

        /// <inheritdoc/>
        async Task<String> IPluginLoader.LoadAsync(IFileInfo file, CancellationToken cancellationToken)
        {
            try
            {
                // Create a plugin context for the provided file
                IPluginContext<TContract> context = CreatePluginContext(file);

                // Add or update the plugin context in the registry
                _registry.AddOrUpdate(context.Id, context, (String _, IPluginContext<TContract> _) => context);

                // Invoke the plugin loaded event
                PluginLoaded?.Invoke(this, context);

                // Return a task with the plugin context's identifier
                return context.Id;
            }
            catch (Exception exception)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.Log(LogLevel.Debug, exception, "Failed to load plugin context from candidate file '{fileName}'", file.Name);

                // Throw
                throw;
            }
        }

        /// <inheritdoc/>
        async Task IPluginLoader.UnloadAsync(String id, CancellationToken cancellationToken)
        {
            try
            {
                // Try to remove the plugin context referenced by the provided id from the registry
                if (_registry.TryRemove(id, out IPluginContext<TContract>? pluginContext) is false)
                    throw new InvalidOperationException($"Could not find registered plugin context '{id}'");

                // Invoke the plugin unloaded event
                PluginUnloaded?.Invoke(this, pluginContext);

                // If the plugin context's provider is async disposable
                if (pluginContext.Provider is IAsyncDisposable asyncDisposable)
                {
                    // Dispose the plugin context's provider
                    await asyncDisposable.DisposeAsync();
                }
                // If the plugin context's provider is disposable
                if (pluginContext.Provider is IDisposable disposable)
                {
                    // Dispose the plugin context's provider
                    disposable.Dispose();
                }

                // Unload the plugin context's assembly load context
                pluginContext.Context.Unload();
            }
            catch (Exception exception)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                    _logger.Log(LogLevel.Warning, exception, "Failed to unload plugin context with id '{id}'", id);

                // Return null
                throw;
            }
        }

        /// <inheritdoc/>
        async Task IPluginLoader.ScanAsync(IFileProvider fileProvider, CancellationToken cancellationToken)
        {
            // Get the directory contents from the file provider's root directory
            IDirectoryContents directoryContents = fileProvider.GetDirectoryContents(String.Empty);

            // Enumerate all sub-directories in the root directory
            IEnumerable<IFileInfo> directories = directoryContents
                // Filter to directory items
                .Where((IFileInfo fileInfo) => fileInfo.IsDirectory is true);

            // Get the implementation as the interface
            IPluginLoader pluginLoader = this;

            // Iterate over enumerated directories
            foreach (IFileInfo directory in directories)
            {
                // Get a task representing the directory scan operation
                await pluginLoader.ScanAsync(fileProvider, directory.Name, cancellationToken);
            }
        }

        /// <inheritdoc/>
        async Task IPluginLoader.ScanAsync(IFileProvider fileProvider, String subpath, CancellationToken cancellationToken)
        {
            // Get the directory contents from the file provider at the provided sub-directory
            IDirectoryContents directoryContents = fileProvider.GetDirectoryContents(subpath);

            // If the file provider's subpath directory does not exist
            if (directoryContents.Exists is false)
                throw new InvalidOperationException($"The provided {nameof(IFileProvider)} does not contain a directory named '{subpath}'");

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.Log(LogLevel.Information, "Scanning directory '{path}' for {contract} plugins", subpath, typeof(TContract).Name);

            // Enumerate all DLL files in the provided directory
            IEnumerable<IFileInfo> files = directoryContents
                // Filter to non-directory items
                .Where((IFileInfo fileInfo) => fileInfo.IsDirectory is false)
                // Filter to files that have a '.dll' extension
                .Where((IFileInfo fileInfo) => fileInfo.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

            // Iterate over enumerated files
            foreach (IFileInfo file in files)
            {
                // Throw an exception if cancellation was requested
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Get the implementation as the interface
                    IPluginLoader pluginLoader = this;

                    // Delegate call to LoadAsync
                    await pluginLoader.LoadAsync(file, cancellationToken);
                }
                catch (Exception exception)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.Log(LogLevel.Debug, exception, "Failed to load plugin context from candidate file '{fileName}'", file.Name);
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.Log(LogLevel.Information, "Scanning directory '{path}' completed: discovered {count} contexts.", subpath, _registry.Count);
        }

        /// <summary>
        /// Loads an <see cref="Assembly"/> from disk, discovers matching plugin 
        /// <see langword="type"/>s, and constructs an isolated 
        /// <see cref="IPluginContext{TContract}"/>.
        /// </summary>
        /// 
        /// <param name="file">
        /// The assembly file info representing the plugin candidate.
        /// </param>
        /// 
        /// <returns>
        /// An initialized <see cref="IPluginContext{TContract}"/> containing the plugin's 
        /// context and dependencies.
        /// </returns>
        /// 
        /// <exception cref="Exception">
        /// Re-throws any exception encountered during type discovery or container creation 
        /// after safely unloading the <see cref="AssemblyLoadContext"/>.
        /// </exception>
        private IPluginContext<TContract> CreatePluginContext(IFileInfo file)
        {
            // Load the provided file and create an assembly load context
            AssemblyLoadContext assemblyLoadContext = _pluginLoadContextFactory.Load(file);

            // Define the service registration strategy
            IServiceRegistrationStrategy strategy = _serviceRegistrationStrategy;

            try
            {
                // Discover plugin types in the assembly load context
                IReadOnlyCollection<Type> types = _pluginTypeDiscoverer.Discover(assemblyLoadContext);

                // Create a service provider for the loaded assembly load context and contained types
                IServiceProvider serviceProvider = _pluginContainerFactory.Create(assemblyLoadContext, types, strategy, _serviceProvider, _serviceDescriptors);

                // Construct a new plugin context instance
                PluginContext<TContract> pluginContext = new(file.Name)
                {
                    Types = types,
                    Context = assemblyLoadContext,
                    Provider = serviceProvider
                };

                // Return the constructed plugin context instance
                return pluginContext;
            }
            catch
            {
                // Unload the assembly load context
                assemblyLoadContext.Unload();

                // Re-throw exception
                throw;
            }
        }
    }
}
