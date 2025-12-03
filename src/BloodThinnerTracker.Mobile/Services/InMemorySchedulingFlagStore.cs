namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Simple in-memory scheduling flag used by unit tests.
    /// </summary>
    public class InMemorySchedulingFlagStore : ISchedulingFlagStore
    {
        private bool _scheduled;

        public bool IsScheduled() => _scheduled;

        public void SetScheduled(bool scheduled) => _scheduled = scheduled;
    }
}
