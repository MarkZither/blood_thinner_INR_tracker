using System;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile.Platforms.Android
{
    /// <summary>
    /// Small static bridge exposing the application's IServiceProvider to
    /// Android runtime-created components (BroadcastReceiver, JobService).
    ///
    /// Usage: call AndroidServiceProvider.Initialize(app.Services) once during
    /// startup (MauiProgram) and then components can call CreateScope()/GetService&lt;T&gt;().
    /// </summary>
    public static class AndroidServiceProvider
    {
        private static IServiceProvider? _provider;

        public static void Initialize(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public static IServiceScope CreateScope()
        {
            if (_provider == null) throw new InvalidOperationException("AndroidServiceProvider not initialized. Call AndroidServiceProvider.Initialize(...) at app startup.");
            return _provider.CreateScope();
        }

        public static T? GetService<T>() where T : class
        {
            return _provider == null ? null : _provider.GetService<T>();
        }
    }
}
