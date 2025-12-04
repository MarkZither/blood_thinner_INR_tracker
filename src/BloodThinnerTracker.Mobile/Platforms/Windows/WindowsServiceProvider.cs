using System;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile.Platforms.Windows
{
    /// <summary>
    /// Small static bridge exposing the application's IServiceProvider to
    /// Windows runtime-created components (background tasks).
    ///
    /// Usage: call WindowsServiceProvider.Initialize(app.Services) once during
    /// startup (MauiProgram) and then components can call CreateScope()/GetService&lt;T&gt;().
    /// </summary>
    public static class WindowsServiceProvider
    {
        private static IServiceProvider? _provider;

        /// <summary>
        /// Initialize the service provider bridge. Call once at app startup.
        /// </summary>
        /// <param name="provider">The application's root service provider.</param>
        public static void Initialize(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Create a new DI scope for background work.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
        public static IServiceScope CreateScope()
        {
            if (_provider == null)
                throw new InvalidOperationException("WindowsServiceProvider not initialized. Call WindowsServiceProvider.Initialize(...) at app startup.");
            return _provider.CreateScope();
        }

        /// <summary>
        /// Get a service directly from the root provider.
        /// Returns null if provider is not initialized or service not registered.
        /// </summary>
        public static T? GetService<T>() where T : class
        {
            return _provider?.GetService<T>();
        }

        /// <summary>
        /// Check if the provider has been initialized.
        /// </summary>
        public static bool IsInitialized => _provider != null;
    }
}
