using Xunit;

namespace BloodThinnerTracker.Api.Tests;

public class DatabaseMigrationLockTests
{
    [Fact(Skip = "Integration test - requires Docker/Testcontainers environment. Run manually when validating migration locking behavior.")]
    public void ConcurrentMigrationAttempts_ShouldSerialize_WhenLocksEnabled()
    {
        // Placeholder: implement with Testcontainers or Docker to spin up a DB and 2 app processes
        // Steps:
        // 1. Start a single Postgres container with a connection string
        // 2. Launch two short-lived processes (or use Testcontainers to run the app entrypoint) that both call EnsureDatabaseAsync
        // 3. Verify that migrations run exactly once and the other process either waits (when PostgresBlock=true) or skips applying concurrently
        // This test is intentionally a manual/integration-level test and is skipped by default.
    }
}
