using System;
using Microsoft.Extensions.DependencyInjection;

namespace BloodThinnerTracker.Mobile.Extensions
{
    /// <summary>
    /// Simple factory that provides a lazy view-model resolver for types resolved from DI.
    /// Allows a view to defer ViewModel creation until the view is actually shown.
    /// </summary>
    public class LazyViewModelFactory<T> where T : class
    {
        private readonly IServiceProvider _sp;

        public LazyViewModelFactory(IServiceProvider sp)
        {
            _sp = sp ?? throw new ArgumentNullException(nameof(sp));
        }

        public Lazy<T> CreateLazy()
        {
            return new Lazy<T>(() => _sp.GetRequiredService<T>());
        }

        public T Create() => _sp.GetRequiredService<T>();
    }
}
