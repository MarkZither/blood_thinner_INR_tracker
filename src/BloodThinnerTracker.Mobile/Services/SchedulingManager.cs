namespace BloodThinnerTracker.Mobile.Services
{
    /// <summary>
    /// Small helper encapsulating scheduling idempotency logic. Keeps unit-testable rules
    /// separate from Android JobScheduler plumbing.
    /// </summary>
    public static class SchedulingManager
    {
        /// <summary>
        /// Returns true if scheduling should proceed.
        /// If the flag store indicates already scheduled and force==false, returns false.
        /// </summary>
        public static bool ShouldSchedule(ISchedulingFlagStore store, bool force)
        {
            if (store == null) return true; // conservative: allow scheduling if no store provided
            var already = store.IsScheduled();
            return force || !already;
        }

        /// <summary>
        /// Mark the store as scheduled.
        /// </summary>
        public static void MarkScheduled(ISchedulingFlagStore store)
        {
            store?.SetScheduled(true);
        }

        /// <summary>
        /// Clear scheduled flag.
        /// </summary>
        public static void ClearScheduled(ISchedulingFlagStore store)
        {
            store?.SetScheduled(false);
        }
    }
}
