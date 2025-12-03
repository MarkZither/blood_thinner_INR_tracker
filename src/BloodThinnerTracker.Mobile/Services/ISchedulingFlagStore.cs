namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Abstraction for storing whether a periodic job has been scheduled.
    /// Implementations may use SharedPreferences (Android) or an in-memory store for tests.
    /// </summary>
    public interface ISchedulingFlagStore
    {
        bool IsScheduled();
        void SetScheduled(bool scheduled);
    }
}
