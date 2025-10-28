using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using BloodThinnerTracker.Api.Data;

namespace BloodThinnerTracker.Api.Services;

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
    /// Determines if the application should use SQLite or PostgreSQL.
    /// </summary>
    bool ShouldUseSqlite(IWebHostEnvironment environment);
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
        
        if (ShouldUseSqlite(environment))
        {
            ConfigureSqlite(options, connectionString, environment);
        }
        else
        {
            ConfigurePostgreSQL(options, connectionString, environment);
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
            sqlite.MigrationsAssembly("BloodThinnerTracker.Api");
            sqlite.CommandTimeout(30);
        });

        _logger.LogInformation("Configured SQLite database for {Environment} environment", environment.EnvironmentName);
    }

    /// <summary>
    /// Configures PostgreSQL for cloud deployment with medical compliance features.
    /// </summary>
    private void ConfigurePostgreSQL(DbContextOptionsBuilder options, string connectionString, IWebHostEnvironment environment)
    {
        options.UseNpgsql(connectionString, postgres =>
        {
            postgres.MigrationsAssembly("BloodThinnerTracker.Api");
            postgres.CommandTimeout(30);
            postgres.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });

        _logger.LogInformation("Configured PostgreSQL database for {Environment} environment", environment.EnvironmentName);
    }

    /// <summary>
    /// Gets the appropriate connection string for the current environment.
    /// </summary>
    public string GetConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (ShouldUseSqlite(environment))
        {
            return GetSqliteConnectionString(configuration, environment);
        }
        else
        {
            return GetPostgreSqlConnectionString(configuration, environment);
        }
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
        var connectionString = configuration.GetConnectionString("PostgreSQLConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Build connection string from individual components
            var server = configuration["Database:PostgreSQL:Server"] ?? "localhost";
            var port = configuration["Database:PostgreSQL:Port"] ?? "5432";
            var database = configuration["Database:PostgreSQL:Database"] ?? $"bloodtracker_{environment.EnvironmentName.ToLower()}";
            var username = configuration["Database:PostgreSQL:Username"] ?? "bloodtracker_user";
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
    /// Determines whether to use SQLite based on environment and configuration.
    /// </summary>
    public bool ShouldUseSqlite(IWebHostEnvironment environment)
    {
        return true;
        // Use SQLite for development by default, PostgreSQL for staging and production
        //return environment.IsDevelopment();
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
        // Register database configuration service
        services.AddSingleton<IDatabaseConfigurationService, DatabaseConfigurationService>();

        // Configure database context
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var databaseConfig = serviceProvider.GetRequiredService<IDatabaseConfigurationService>();
            databaseConfig.ConfigureDatabase(options, configuration, environment);
        });

        // Configure data protection with database storage
        services.AddDataProtection(options =>
        {
            options.ApplicationDiscriminator = "BloodThinnerTracker";
        })
        .PersistKeysToDbContext<ApplicationDbContext>()
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90)) // 90-day key rotation for medical data
        .SetApplicationName("BloodThinnerTracker");

        // Add health checks for database
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database",
                customTestQuery: async (context, cancellationToken) =>
                {
                    // Test database connectivity with a simple query
                    return await context.Database.CanConnectAsync(cancellationToken);
                });

        return services;
    }

    /// <summary>
    /// Ensures database is created and migrations are applied.
    /// </summary>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        try
        {
            // Check if database exists and has tables
            var canConnect = await context.Database.CanConnectAsync();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            
            if (!canConnect || pendingMigrations.Any())
            {
                logger.LogInformation("Initializing medical database...");
                
                // Apply pending migrations (this will create the database if it doesn't exist)
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending database migrations", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");
                }
                else
                {
                    // If no migrations are pending but database doesn't exist, create it
                    await context.Database.EnsureCreatedAsync();
                    logger.LogInformation("Database created successfully");
                }
            }
            else
            {
                logger.LogInformation("Database is up to date");
            }

            // Log database information
            var databaseProvider = context.Database.ProviderName;
            logger.LogInformation("Medical database initialized successfully using {Provider} for {Environment}",
                databaseProvider, environment.EnvironmentName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize medical database");
            throw;
        }
    }
}