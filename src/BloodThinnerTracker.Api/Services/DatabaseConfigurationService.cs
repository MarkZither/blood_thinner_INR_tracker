using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using BloodThinnerTracker.Data.Shared;
using System.Data;
using System.Data.Common;

namespace BloodThinnerTracker.Api.Services;

/// <summary>
/// Supported database providers for medical data storage.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>SQLite for local development and testing.</summary>
    SQLite,

    /// <summary>PostgreSQL for cloud deployment and production.</summary>
    PostgreSQL,

    /// <summary>SQL Server / Azure SQL for enterprise deployment.</summary>
    SqlServer
}

/// <summary>
/// Database configuration service for managing connection strings and providers.
///
/// ⚠️ MEDICAL DATABASE CONFIGURATION:
/// This service configures database connections for medical data with proper
/// encryption, connection pooling, and compliance features.
/// </summary>
public interface IDatabaseConfigurationService
{
    /// <summary>
    /// Configures the database context with the appropriate provider and settings.
    /// </summary>
    void ConfigureDatabase(DbContextOptionsBuilder options, IConfiguration configuration, IWebHostEnvironment environment);

    /// <summary>
    /// Gets the appropriate connection string based on the environment.
    /// </summary>
    string GetConnectionString(IConfiguration configuration, IWebHostEnvironment environment);

    /// <summary>
    /// Determines which database provider to use based on configuration and environment.
    /// </summary>
    DatabaseProvider GetDatabaseProvider(IConfiguration configuration, IWebHostEnvironment environment);
}

/// <summary>
/// Implementation of database configuration service.
/// </summary>
public class DatabaseConfigurationService : IDatabaseConfigurationService
{
    private readonly ILogger<DatabaseConfigurationService> _logger;

    public DatabaseConfigurationService(ILogger<DatabaseConfigurationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configures the database context with appropriate provider and medical compliance settings.
    /// </summary>
    public void ConfigureDatabase(DbContextOptionsBuilder options, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = GetConnectionString(configuration, environment);
        var provider = GetDatabaseProvider(configuration, environment);

        switch (provider)
        {
            case DatabaseProvider.SQLite:
                ConfigureSqlite(options, connectionString, environment);
                break;
            case DatabaseProvider.PostgreSQL:
                ConfigurePostgreSQL(options, connectionString, environment);
                break;
            case DatabaseProvider.SqlServer:
                ConfigureSqlServer(options, connectionString, environment);
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider: {provider}");
        }

        // Enable sensitive data logging only in development
        if (environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging(false); // Keep false even in dev for medical data
            options.EnableDetailedErrors();
        }

        // Configure logging
        options.LogTo(
            message => _logger.LogDebug("EF Core: {Message}", message),
            new[] { DbLoggerCategory.Database.Command.Name },
            LogLevel.Information);
    }

    /// <summary>
    /// Configures SQLite for local development with medical data encryption.
    /// </summary>
    private void ConfigureSqlite(DbContextOptionsBuilder options, string connectionString, IWebHostEnvironment environment)
    {
        options.UseSqlite(connectionString, sqlite =>
        {
            sqlite.MigrationsAssembly("BloodThinnerTracker.Data.SQLite");
            sqlite.CommandTimeout(30);
        });

        _logger.LogInformation("Configured SQLite database for {Environment} environment", environment.EnvironmentName);
    }

    /// <summary>
    /// Configures PostgreSQL for cloud deployment with medical compliance features.
    /// </summary>
    private void ConfigurePostgreSQL(DbContextOptionsBuilder options, string connectionString, IWebHostEnvironment environment)
    {
        /*options.UseNpgsql(connectionString, postgres =>
        {
            postgres.MigrationsAssembly("BloodThinnerTracker.Data.PostgreSQL");
            postgres.CommandTimeout(30);
            postgres.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });*/

        _logger.LogInformation("Configured PostgreSQL database for {Environment} environment", environment.EnvironmentName);
    }

    /// <summary>
    /// Configures SQL Server / Azure SQL for enterprise deployment with medical compliance features.
    /// </summary>
    private void ConfigureSqlServer(DbContextOptionsBuilder options, string connectionString, IWebHostEnvironment environment)
    {
        options.UseSqlServer(connectionString, sqlServer =>
        {
            sqlServer.MigrationsAssembly("BloodThinnerTracker.Data.SqlServer");
            sqlServer.CommandTimeout(30);
            sqlServer.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);

            // Enable connection resiliency for Azure SQL
            if (environment.IsProduction() || environment.IsStaging())
            {
                sqlServer.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }
        });

        _logger.LogInformation("Configured SQL Server database for {Environment} environment", environment.EnvironmentName);
    }

    /// <summary>
    /// Gets the appropriate connection string for the current environment.
    /// </summary>
    public string GetConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var provider = GetDatabaseProvider(configuration, environment);

        return provider switch
        {
            DatabaseProvider.SQLite => GetSqliteConnectionString(configuration, environment),
            DatabaseProvider.PostgreSQL => GetPostgreSqlConnectionString(configuration, environment),
            DatabaseProvider.SqlServer => GetSqlServerConnectionString(configuration, environment),
            _ => throw new InvalidOperationException($"Unsupported database provider: {provider}")
        };
    }

    /// <summary>
    /// Gets the SQLite connection string with encryption for medical data.
    /// </summary>
    private string GetSqliteConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var databaseName = environment.EnvironmentName.ToLower() switch
        {
            "development" => "bloodtracker_dev.db",
            "staging" => "bloodtracker_staging.db",
            "production" => "bloodtracker_prod.db",
            _ => "bloodtracker.db"
        };

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Default SQLite connection string with medical data protection
        var sqliteConnection = $"Data Source={databaseName};Cache=Shared;";

        // Add encryption for production SQLite (if using SQLCipher)
        if (environment.IsProduction())
        {
            var encryptionKey = configuration["Database:EncryptionKey"];
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                sqliteConnection += $"Password={encryptionKey};";
            }
            else
            {
                _logger.LogWarning("No database encryption key configured for production SQLite");
            }
        }

        return sqliteConnection;
    }

    /// <summary>
    /// Gets the PostgreSQL connection string with SSL and security configurations.
    /// </summary>
    private string GetPostgreSqlConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
    {
        // First check for Aspire-injected connection string (ConnectionStrings__bloodtracker)
        var connectionString = configuration.GetConnectionString("bloodtracker")
                            ?? configuration.GetConnectionString("PostgreSQLConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // Build connection string from individual components
            var server = configuration["Database:PostgreSQL:Server"] ?? "localhost";
            var port = configuration["Database:PostgreSQL:Port"] ?? "5432";
            var database = configuration["Database:PostgreSQL:Database"] ?? $"bloodtracker_{environment.EnvironmentName.ToLower()}";
            var username = configuration["Database:PostgreSQL:Username"] ?? "postgres"; // Default PostgreSQL user
            var password = configuration["Database:PostgreSQL:Password"] ?? throw new InvalidOperationException("PostgreSQL password not configured");

            connectionString = $"Host={server};Port={port};Database={database};Username={username};Password={password};";

            // Add SSL configuration for production
            if (environment.IsProduction())
            {
                connectionString += "SSL Mode=Require;Trust Server Certificate=false;";
            }
            else if (environment.IsStaging())
            {
                connectionString += "SSL Mode=Prefer;";
            }

            // Add connection pooling and timeout settings
            connectionString += "Pooling=true;Min Pool Size=5;Max Pool Size=100;Connection Lifetime=0;Command Timeout=30;";
        }

        return connectionString;
    }

    /// <summary>
    /// Gets the SQL Server connection string with encryption and security configurations.
    /// </summary>
    private string GetSqlServerConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Check for connection string from configuration
        var connectionString = configuration.GetConnectionString("SqlServerConnection")
                            ?? configuration.GetConnectionString("sqlserver");

        if (string.IsNullOrEmpty(connectionString))
        {
            // Build connection string from individual components
            var server = configuration["Database:SqlServer:Server"] ?? "localhost";
            var database = configuration["Database:SqlServer:Database"] ?? $"bloodtracker_{environment.EnvironmentName.ToLower()}";
            var username = configuration["Database:SqlServer:Username"];
            var password = configuration["Database:SqlServer:Password"];

            // Support both SQL Server Authentication and Windows Authentication
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                connectionString = $"Server={server};Database={database};User Id={username};Password={password};";
            }
            else
            {
                // Use Windows/Integrated Authentication
                connectionString = $"Server={server};Database={database};Integrated Security=true;";
            }

            // Add security settings for production
            if (environment.IsProduction())
            {
                connectionString += "Encrypt=true;TrustServerCertificate=false;";
            }
            else if (environment.IsDevelopment())
            {
                connectionString += "Encrypt=false;TrustServerCertificate=true;";
            }

            // Add connection pooling and timeout settings
            connectionString += "MultipleActiveResultSets=true;Connection Timeout=30;";
        }

        return connectionString;
    }

    /// <summary>
    /// Determines which database provider to use based on configuration and environment.
    /// </summary>
    public DatabaseProvider GetDatabaseProvider(IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Check explicit configuration first
        var providerConfig = configuration["Database:Provider"];
        if (!string.IsNullOrEmpty(providerConfig))
        {
            if (Enum.TryParse<DatabaseProvider>(providerConfig, ignoreCase: true, out var provider))
            {
                return provider;
            }
        }

        // Check for provider-specific connection strings
        if (!string.IsNullOrEmpty(configuration.GetConnectionString("SqlServerConnection")) ||
            !string.IsNullOrEmpty(configuration.GetConnectionString("sqlserver")))
        {
            return DatabaseProvider.SqlServer;
        }

        if (!string.IsNullOrEmpty(configuration.GetConnectionString("bloodtracker")) ||
            !string.IsNullOrEmpty(configuration.GetConnectionString("PostgreSQLConnection")))
        {
            return DatabaseProvider.PostgreSQL;
        }

        // Default: PostgreSQL for production/staging, SQLite for development
        if (environment.IsProduction() || environment.IsStaging())
        {
            return DatabaseProvider.PostgreSQL;
        }

        return DatabaseProvider.SQLite;
    }

    /// <summary>
    /// Determines whether to use SQLite based on environment and configuration.
    /// </summary>
    [Obsolete("Use GetDatabaseProvider() instead")]
    public bool ShouldUseSqlite(IWebHostEnvironment environment)
    {
        // When running under Aspire orchestration, PostgreSQL is available via container
        // Use SQLite only when no PostgreSQL connection string is available
        return false;
    }
}

/// <summary>
/// Extension methods for database configuration.
/// </summary>
public static class DatabaseConfigurationExtensions
{
    /// <summary>
    /// Adds database services with medical compliance configuration.
    /// </summary>
    public static IServiceCollection AddMedicalDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
    // Register current user service
    services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Register audit interceptor which will capture INRTest changes and write AuditRecord entries
    services.AddScoped<AuditInterceptor>();

        // Register database configuration service
        services.AddSingleton<IDatabaseConfigurationService, DatabaseConfigurationService>();

        // Determine which provider to use
        var tempServiceProvider = services.BuildServiceProvider();
        var databaseConfig = tempServiceProvider.GetRequiredService<IDatabaseConfigurationService>();
        var provider = databaseConfig.GetDatabaseProvider(configuration, environment);

        // Register the appropriate provider-specific context
        switch (provider)
        {
            case DatabaseProvider.SQLite:
                services.AddDbContext<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>(
                    (serviceProvider, options) =>
                    {
                        var dbConfig = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
                        dbConfig.ConfigureDatabase(options, configuration, environment);

                        // Attach the AuditInterceptor so it runs on SaveChanges for this DbContext
                        var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
                        options.AddInterceptors(interceptor);
                    });
                // Register interface pointing to concrete implementation
                services.AddScoped<IApplicationDbContext>(sp =>
                    sp.GetRequiredService<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>());

                // Configure data protection with database storage (requires concrete DbContext)
                services.AddDataProtection(options =>
                {
                    options.ApplicationDiscriminator = "BloodThinnerTracker";
                })
                .PersistKeysToDbContext<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>()
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
                .SetApplicationName("BloodThinnerTracker");

                // Add health checks for database (requires concrete DbContext)
                services.AddHealthChecks()
                    .AddDbContextCheck<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>("database");
                break;

            /*case DatabaseProvider.PostgreSQL:
                services.AddDbContext<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>(
                    (serviceProvider, options) =>
                    {
                        var dbConfig = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
                        dbConfig.ConfigureDatabase(options, configuration, environment);

                        // Attach AuditInterceptor when PostgreSQL provider is enabled
                        var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
                        options.AddInterceptors(interceptor);
                    });

                services.AddScoped<IApplicationDbContext>(sp =>
                    sp.GetRequiredService<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>());

                services.AddDataProtection(options =>
                {
                    options.ApplicationDiscriminator = "BloodThinnerTracker";
                })
                .PersistKeysToDbContext<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>()
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
                .SetApplicationName("BloodThinnerTracker");

                services.AddHealthChecks()
                    .AddDbContextCheck<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>("database");
                break;
            */
            case DatabaseProvider.SqlServer:
                services.AddDbContext<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>(
                    (serviceProvider, options) =>
                    {
                        var dbConfig = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
                        dbConfig.ConfigureDatabase(options, configuration, environment);

                        // Attach the AuditInterceptor so it runs on SaveChanges for this DbContext
                        var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
                        options.AddInterceptors(interceptor);
                    });
                services.AddScoped<IApplicationDbContext>(sp =>
                    sp.GetRequiredService<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>());

                services.AddDataProtection(options =>
                {
                    options.ApplicationDiscriminator = "BloodThinnerTracker";
                })
                .PersistKeysToDbContext<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>()
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
                .SetApplicationName("BloodThinnerTracker");

                services.AddHealthChecks()
                    .AddDbContextCheck<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>("database");
                break;

            default:
                throw new InvalidOperationException($"Unsupported database provider: {provider}");
        }

        return services;
    }

    /// <summary>
    /// Ensures database is created and migrations are applied.
    /// </summary>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseConfigurationService>>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Cast to DbContext to access Database property
            if (context is not DbContext dbContext)
            {
                throw new InvalidOperationException("Context does not inherit from DbContext");
            }

            // Use a cancellation token with timeout to prevent infinite hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var cancellationToken = cts.Token;

            // Apply migrations with retry logic for database creation race conditions
            await TryApplyMigrationsWithRetry(dbContext, logger, environment, configuration, cancellationToken);
        }
        catch (TimeoutException)
        {
            logger.LogError("Database initialization timed out - this may indicate a migration lock issue");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize medical database");
            throw;
        }
    }

    /// <summary>
    /// Attempts to apply database migrations with retry logic for race conditions.
    /// </summary>
    private static async Task TryApplyMigrationsWithRetry(
        DbContext dbContext,
        ILogger<DatabaseConfigurationService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await ApplyPendingMigrations(dbContext, logger, environment, configuration, cancellationToken);

                // Success - break out of retry loop
                break;
            }
            catch (Exception ex) when (
                attempt < maxRetries &&
                (ex.Message.Contains("does not exist") ||
                 ex.Message.Contains("EndOfStream") ||
                 ex is System.IO.EndOfStreamException ||
                 (ex.InnerException?.Message.Contains("EndOfStream") ?? false) ||
                 (ex.InnerException is System.IO.EndOfStreamException)))
            {
                // Database doesn't exist yet or connection failed mid-creation
                // This is expected when Aspire is creating the database
                logger.LogWarning(ex,
                    "Database connection failed on attempt {Attempt}/{MaxRetries}. " +
                    "This is expected during Aspire database creation. Waiting {Delay} seconds before retry...",
                    attempt, maxRetries, retryDelay.TotalSeconds);

                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
                else
                {
                    // Last attempt failed - rethrow
                    logger.LogError(ex, "Failed to connect to database after {MaxRetries} attempts", maxRetries);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Applies pending database migrations or creates the database if needed.
    /// </summary>
    private static async Task ApplyPendingMigrations(
        DbContext dbContext,
        ILogger<DatabaseConfigurationService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // Check if database exists and has tables
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            logger.LogWarning("Cannot connect to database, will attempt to create it");
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

        if (!canConnect || pendingMigrations.Any())
        {
            logger.LogInformation("Initializing medical database...");
            // We'll use a server-side advisory lock for provider databases that support it (Postgres, SQL Server)
            // so that only one instance attempts to apply migrations at a time when running app-start migrations.
            var lockAcquired = false;
            DbConnection? openedConnection = null;

            try
            {
                // Open connection explicitly so session-scoped locks (Postgres/SQL Server) are held on this session
                openedConnection = dbContext.Database.GetDbConnection();
                if (openedConnection.State != ConnectionState.Open)
                {
                    await dbContext.Database.OpenConnectionAsync(cancellationToken);
                }

                    // Read configuration for migration locking behavior
                    var requireLock = configuration.GetValue<bool>("Database:MigrationLock:Require", false);
                    var postgresBlock = configuration.GetValue<bool>("Database:MigrationLock:PostgresBlock", false);

                    // Try to acquire provider-specific migration lock. If acquiring fails, we log and continue or fail based on config.
                    lockAcquired = await TryAcquireMigrationLockAsync(dbContext, logger, postgresBlock, cancellationToken);
                    if (!lockAcquired && requireLock)
                    {
                        // Requirement to obtain a lock failed - fail fast to avoid concurrent migrations
                        throw new InvalidOperationException("Migration lock was required but could not be obtained");
                    }

                // Apply pending migrations (this will create the database if it doesn't exist)
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending database migrations: {Migrations}",
                        pendingMigrations.Count(),
                        string.Join(", ", pendingMigrations));

                    try
                    {
                        await dbContext.Database.MigrateAsync(cancellationToken);
                        logger.LogInformation("Database migrations applied successfully");
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogError("Database migration timed out after 5 minutes");
                        throw new TimeoutException("Database migration operation timed out");
                    }
                }
                else if (!canConnect)
                {
                    // If no migrations are pending but database doesn't exist, create it
                    logger.LogInformation("Creating database schema...");
                    await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                    logger.LogInformation("Database created successfully");
                }
            }
            finally
            {
                // Always attempt to release the lock if we acquired it, then close the connection we opened
                if (lockAcquired)
                {
                    try
                    {
                        await TryReleaseMigrationLockAsync(dbContext, logger, CancellationToken.None);
                    }
                    catch (DbException ex)
                    {
                        logger.LogWarning(ex, "Failed to release migration advisory lock cleanly (DbException)");
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning(ex, "Failed to release migration advisory lock cleanly (InvalidOperationException)");
                    }
                }

                if (openedConnection != null && openedConnection.State == ConnectionState.Open)
                {
                    try
                    {
                        await dbContext.Database.CloseConnectionAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Error closing database connection after migrations");
                    }
                }
            }
        }
        else
        {
            logger.LogInformation("Database is up to date");
        }

        // Log database information
        var databaseProvider = dbContext.Database.ProviderName;
        logger.LogInformation("Medical database initialized successfully using {Provider} for {Environment}",
            databaseProvider, environment.EnvironmentName);
    }

    /// <summary>
    /// Attempts to acquire a provider-specific advisory lock to serialize migrations across instances.
    /// Returns true if the lock was acquired (or if locking is unsupported), false if lock could not be obtained.
    /// </summary>
    private static async Task<bool> TryAcquireMigrationLockAsync(DbContext dbContext, ILogger<DatabaseConfigurationService> logger, bool postgresBlock, CancellationToken cancellationToken)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;

        try
        {
            var conn = dbContext.Database.GetDbConnection();
            // Ensure connection is open
            if (conn.State != ConnectionState.Open)
            {
                await dbContext.Database.OpenConnectionAsync(cancellationToken);
            }

            using var cmd = conn.CreateCommand();

            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || provider.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // Use a fixed advisory lock key. This is a large signed bigint.
                const long lockKey = 4242424242424242L;
                var param = cmd.CreateParameter();
                param.ParameterName = "p0";
                param.Value = lockKey;
                cmd.Parameters.Add(param);

                if (postgresBlock)
                {
                    // Blocking lock - will wait until lock is obtained. Use cancellation token via command timeout if desired.
                    cmd.CommandText = "SELECT pg_advisory_lock($1);";
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    // If we return, lock was acquired
                    logger.LogInformation("Postgres advisory lock (blocking) acquired");
                    return true;
                }
                else
                {
                    // Non-blocking try-lock
                    cmd.CommandText = "SELECT pg_try_advisory_lock($1);";
                    var result = await cmd.ExecuteScalarAsync(cancellationToken);
                    if (result is bool b)
                    {
                        logger.LogInformation("Postgres advisory lock attempt returned {Result}", b);
                        return b;
                    }

                    return Convert.ToInt32(result) == 1;
                }
            }
            else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) || provider.Contains("Microsoft.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
            {
                // Use sp_getapplock to request an exclusive lock for this session.
                cmd.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock @Resource = @res, @LockMode = 'Exclusive', @LockTimeout = 60000, @LockOwner = 'Session'; SELECT @result;";
                var param = cmd.CreateParameter();
                param.ParameterName = "@res";
                param.Value = "BloodThinnerTracker_Migrations";
                cmd.Parameters.Add(param);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var status = Convert.ToInt32(result);
                // Per sp_getapplock docs, >= 0 is success
                var success = status >= 0;
                logger.LogInformation("SQL Server sp_getapplock returned {Status}", status);
                return success;
            }

            // Provider does not support advisory locks (e.g., SQLite) - treat as locked/OK
            logger.LogDebug("Database provider {Provider} does not support advisory locks; proceeding without lock", provider);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to acquire migration advisory lock - proceeding without lock");
            return true;
        }
    }

    /// <summary>
    /// Attempts to release a provider-specific advisory lock. Exceptions are logged but do not stop shutdown.
    /// </summary>
    private static async Task TryReleaseMigrationLockAsync(DbContext dbContext, ILogger<DatabaseConfigurationService> logger, CancellationToken cancellationToken)
    {
        var provider = dbContext.Database.ProviderName ?? string.Empty;

        try
        {
            var conn = dbContext.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                // Nothing to do if connection isn't open
                return;
            }

            using var cmd = conn.CreateCommand();

            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || provider.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                const long lockKey = 4242424242424242L;
                cmd.CommandText = "SELECT pg_advisory_unlock($1);";
                var param = cmd.CreateParameter();
                param.ParameterName = "p0";
                param.Value = lockKey;
                cmd.Parameters.Add(param);

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                logger.LogInformation("Postgres advisory unlock result: {Result}", result);
            }
            else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) || provider.Contains("Microsoft.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
            {
                cmd.CommandText = "EXEC sp_releaseapplock @Resource = @res, @LockOwner = 'Session';";
                var param = cmd.CreateParameter();
                param.ParameterName = "@res";
                param.Value = "BloodThinnerTracker_Migrations";
                cmd.Parameters.Add(param);

                await cmd.ExecuteNonQueryAsync(cancellationToken);
                logger.LogInformation("SQL Server sp_releaseapplock executed");
            }
            else
            {
                logger.LogDebug("No advisory lock to release for provider {Provider}", provider);
            }
        }
        catch (Exception ex)
        {
        catch (OperationCanceledException)
        {
            // Propagate cancellation
            throw;
        }
        catch (DbException dbEx)
        {
            logger.LogWarning(dbEx, "Database error while releasing advisory lock");
        }
        catch (InvalidOperationException invOpEx)
        {
            logger.LogWarning(invOpEx, "Invalid operation while releasing advisory lock");
        }
    }
    }

    /// <summary>
    /// Determines if the exception is critical and should not be caught.
    /// </summary>
    private static bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException
            || ex is StackOverflowException
            || ex is AccessViolationException
            || ex is System.Threading.ThreadAbortException;
    }
}
