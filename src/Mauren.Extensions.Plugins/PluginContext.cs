using Mauren.Extensions.Plugins.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.Loader;

namespace Mauren.Extensions.Plugins
{
    /// <inheritdoc/>
    internal class PluginContext<TContract>(String id) : IPluginContext<TContract>
    {
        /// <inheritdoc/>
        public String Id { get; } = id;

        /// <inheritdoc/>
        public required IEnumerable<Type> Types { get; init; }

        /// <inheritdoc/>
        public required AssemblyLoadContext Context { get; init; }

        /// <inheritdoc/>
        public required IServiceProvider Provider { get; init; }
    }
}
