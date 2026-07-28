# Mauren.Extensions.Plugins

<strong>Mauren.Extensions.Plugins</strong> provides a robust, extensible engine for building pluggable .NET applications.

Key Features:
- <strong>Context Isolation</strong>: Safely load and unload plugin assemblies on the fly using standard <code>AssemblyLoadContext</code> mechanics.
- <strong>Child Dependency Injection</strong>: Automatically generate isolated <code>IServiceProvider</code> scopes for plugins while selectively propagating host-level singletons.
- <strong>Native Hosting Integration</strong>: Automatically discover and orchestrate <code>IHostedService</code> background workers bundled within plugins.
- <strong>Contract-First Design</strong>: Fully decoupled abstraction layers ensure your host applications and plugin authors never tightly couple to the loading engine.
