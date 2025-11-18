**App-Start Migrations & Advisory Locking**

Summary
- App-start migrations are supported and will continue to be the default migration strategy.
- To make app-start migrations safe when multiple instances start at once, the application now attempts to acquire a server-side advisory lock before applying migrations.

Supported providers
- PostgreSQL: uses `pg_try_advisory_lock` by default (non-blocking). Optionally can use `pg_advisory_lock` (blocking) when configured.
- SQL Server: uses `sp_getapplock` / `sp_releaseapplock` to serialize migrations across sessions.
- SQLite: file-backed / in-process; advisory locks are not applicable. SQLite continues to use `EnsureCreated` or migrations as usual.

Configuration
- `Database:MigrationLock:Require` (bool): When `true`, startup will fail if the migration lock cannot be obtained. Default: `false`.
- `Database:MigrationLock:PostgresBlock` (bool): When `true`, PostgreSQL will use `pg_advisory_lock` (blocking) instead of `pg_try_advisory_lock`. Default: `false`.

Examples (appsettings.Development.json)
```json
{
  "Database": {
    "MigrationLock": {
      "Require": false,
      "PostgresBlock": false
    }
  }
}
```

Behavior notes
- The application opens a dedicated DB connection and acquires the advisory lock on that session before applying EF Core migrations. After migrations complete the lock is released and the connection is closed.
- If `Require` is `true` and a lock cannot be obtained, startup will throw and the process will exit. This is useful in environments where you want a single orchestrator to be responsible for migrations.
- `PostgresBlock=true` will cause the process to block on `pg_advisory_lock` until the lock is available. Be careful if startup timeouts are configured — a long wait may delay readiness.

Permissions
- PostgreSQL: `pg_try_advisory_lock` and `pg_advisory_lock` don't require special privileges beyond normal DB connect rights.
- SQL Server: `sp_getapplock`/`sp_releaseapplock` require appropriate permissions. In many environments the `db_owner` role or explicit EXECUTE on the stored procedure is sufficient. Verify with your DBA/security team.

Testing & Validation
- The repository includes a skipped integration test placeholder (`DatabaseMigrationLockTests`) showing an approach to validate lock behavior with Docker/Testcontainers. Implementing full concurrent-instance tests requires an environment that can run multiple isolated containers and is therefore marked as integration-level.

Recommendations
- For single-instance or simple deployments, default non-blocking lock behavior is fine.
- For production clusters, recommended approaches (choose one):
  - Run migrations as a separate CI/CD step or one-shot Job before deploying new application instances.
  - If you must rely on app-start migrations, enable advisory locks and set `Require=true` so startup fails instead of risking concurrent migrations.

Rollout plan
1. Validate migrations in staging with `PostgresBlock=false` and `Require=false` to ensure no regressions.
2. Consider enabling `PostgresBlock=true` for a short period in a maintenance window to validate locking behavior.
3. For multi-node production, prefer CI/CD-run migrations or dedicated migration jobs.

References
- PostgreSQL advisory locks: https://www.postgresql.org/docs/current/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS
- SQL Server `sp_getapplock`: https://learn.microsoft.com/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql
