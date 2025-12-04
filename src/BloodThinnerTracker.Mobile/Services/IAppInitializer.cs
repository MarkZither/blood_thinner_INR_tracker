using System;
using System.Threading.Tasks;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface IAppInitializer
    {
        Task InitializeAsync(TimeSpan timeout);
    }
}
