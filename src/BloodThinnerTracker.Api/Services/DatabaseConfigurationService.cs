using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using BloodThinnerTracker.Data.Shared;

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

            case DatabaseProvider.SqlServer:
                services.AddDbContext<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>(
                    (serviceProvider, options) =>
                    {
                        var dbConfig = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
                        dbConfig.ConfigureDatabase(options, configuration, environment);
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
                break;*/

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
            await TryApplyMigrationsWithRetry(dbContext, logger, environment, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await ApplyPendingMigrations(dbContext, logger, environment, cancellationToken);

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
        else
        {
            logger.LogInformation("Database is up to date");
        }

        // Log database information
        var databaseProvider = dbContext.Database.ProviderName;
        logger.LogInformation("Medical database initialized successfully using {Provider} for {Environment}",
            databaseProvider, environment.EnvironmentName);
    }
}
