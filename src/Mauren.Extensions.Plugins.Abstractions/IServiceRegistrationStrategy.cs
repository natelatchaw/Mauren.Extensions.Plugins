using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mauren.Extensions.Plugins.Abstractions
{
    /// <summary>
    /// 
    /// </summary>
    public interface IServiceRegistrationStrategy
    {
        /// <summary>
        /// A <see cref="Predicate{T}"/> defining a set of criteria for determining
        /// if a given <see cref="Type"/> can provide service registration.
        /// </summary>
        Predicate<Type> ProvidesServices { get; }

        /// <summary>
        /// Applies the provided <paramref name="type"/>'s registration logic
        /// to the provided <paramref name="services"/>.
        /// </summary>
        /// 
        /// <param name="type">
        /// A <see cref="Type"/> potentially containing logic for service
        /// registration. The provided value meets the criteria specified
        /// by <see cref="ProvidesServices"/>.
        /// </param>
        /// 
        /// <param name="services">
        /// The service collection to apply registration logic to.
        /// </param>
        void Apply(Type type, IServiceCollection services);
    }
}
