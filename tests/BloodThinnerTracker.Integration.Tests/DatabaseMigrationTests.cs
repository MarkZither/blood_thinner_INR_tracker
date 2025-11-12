using BloodThinnerTracker.Data.Shared;
using BloodThinnerTracker.Shared.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace BloodThinnerTracker.Integration.Tests;

/// <summary>
/// Integration tests for database migrations across different database providers.
/// Uses Testcontainers to spin up real database instances for testing.
/// </summary>
public class DatabaseMigrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private string? _postgresConnectionString;
    private MsSqlContainer? _sqlServerContainer;
    private string? _sqlServerConnectionString;
    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();
        _postgresConnectionString = _postgresContainer.GetConnectionString();

        // Start SQL Server container
        _sqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();

        await _sqlServerContainer.StartAsync();
        _sqlServerConnectionString = _sqlServerContainer.GetConnectionString();

        // Setup DI container with required services
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddScoped<ICurrentUserService, TestCurrentUserService>();
        services.AddLogging(builder => builder.AddConsole());
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
        if (_sqlServerContainer != null)
        {
            await _sqlServerContainer.DisposeAsync();
        }
        _serviceProvider?.Dispose();
    }
    /*
    private BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext CreatePostgreSqlContext(DbContextOptions<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext> options)
    {
        var dataProtectionProvider = _serviceProvider!.GetRequiredService<IDataProtectionProvider>();
        var currentUserService = _serviceProvider!.GetRequiredService<ICurrentUserService>();
        var logger = _serviceProvider!.GetRequiredService<ILogger<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>>();

        return new BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext(options, dataProtectionProvider, currentUserService, logger);
    }*/

    private BloodThinnerTracker.Data.SqlServer.ApplicationDbContext CreateSqlServerContext(DbContextOptions<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext> options)
    {
        var dataProtectionProvider = _serviceProvider!.GetRequiredService<IDataProtectionProvider>();
        var currentUserService = _serviceProvider!.GetRequiredService<ICurrentUserService>();
        var logger = _serviceProvider!.GetRequiredService<ILogger<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>>();

        return new BloodThinnerTracker.Data.SqlServer.ApplicationDbContext(options, dataProtectionProvider, currentUserService, logger);
    }

    private BloodThinnerTracker.Data.SQLite.ApplicationDbContext CreateSQLiteContext(DbContextOptions<BloodThinnerTracker.Data.SQLite.ApplicationDbContext> options)
    {
        var dataProtectionProvider = _serviceProvider!.GetRequiredService<IDataProtectionProvider>();
        var currentUserService = _serviceProvider!.GetRequiredService<ICurrentUserService>();
        var logger = _serviceProvider!.GetRequiredService<ILogger<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>>();

        return new BloodThinnerTracker.Data.SQLite.ApplicationDbContext(options, dataProtectionProvider, currentUserService, logger);
    }
    /*
    [Fact]
    public async Task PostgreSQL_MigrationsApplySuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>().UseNpgsql(_postgresConnectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        using var context = CreatePostgreSqlContext(options);

        // Act - Apply migrations
        await context.Database.MigrateAsync();

        // Assert - Verify database was created and migrations applied
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect, "Should be able to connect to PostgreSQL database");

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(appliedMigrations);
    }

    [Fact]
    public async Task PostgreSQL_AllTablesCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>().UseNpgsql(_postgresConnectionString)
            .Options;

        using var context = CreatePostgreSqlContext(options);
        await context.Database.MigrateAsync();

        // Act - Query for tables
        var tables = await context.Database
            .SqlQuery<string>($"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
            .ToListAsync();

        // Assert - Verify all expected tables exist
        var expectedTables = new[]
        {
            "Users",
            "Medications",
            "MedicationLogs",
            "INRTests",
            "INRSchedules",
            "RefreshTokens",
            "AuditLogs",
            "DataProtectionKeys",
            "__EFMigrationsHistory"
        };

        foreach (var expectedTable in expectedTables)
        {
            Assert.Contains(expectedTable, tables, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task PostgreSQL_CheckConstraintsWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>().UseNpgsql(_postgresConnectionString)
            .Options;

        using var context = CreatePostgreSqlContext(options);
        await context.Database.MigrateAsync();

        // Act & Assert - Create a test user first (PublicId is GUID, but Id is int identity)
        // Note: User is the root tenant entity and does NOT have a UserId column
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""Users"" (""PublicId"", ""Name"", ""Email"", ""AuthProvider"", ""Role"", ""PreferredLanguage"", ""TimeZone"", ""IsActive"", ""EmailVerified"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"", ""ReminderAdvanceMinutes"", ""IsEmailNotificationsEnabled"", ""IsPushNotificationsEnabled"", ""IsSmsNotificationsEnabled"", ""TwoFactorEnabled"")
            VALUES (gen_random_uuid(), 'Test User', 'test@example.com', 'Local', 'Patient', 'en-US', 'UTC', true, true, NOW(), NOW(), false, 30, true, true, false, false);
        ");

        // Should throw when violating INR value constraint (< 0.5)
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO ""INRTests"" (""PublicId"", ""UserId"", ""TestDate"", ""INRValue"", ""Status"", ""IsPointOfCare"", ""ReviewedByProvider"", ""PatientNotified"", ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"")
                VALUES (gen_random_uuid(), (SELECT ""Id"" FROM ""Users"" LIMIT 1), NOW(), 0.3, 0, false, false, false, NOW(), NOW(), false);
            ");
        });

        Assert.Contains("CK_INRTest_Value", exception.Message);
    }

    [Fact]
    public async Task PostgreSQL_ColumnNamesAreCaseCorrect()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>().UseNpgsql(_postgresConnectionString)
            .Options;

        using var context = CreatePostgreSqlContext(options);
        await context.Database.MigrateAsync();

        // Act - Query for columns with specific names that had casing issues
        var columns = await context.Database
            .SqlQuery<string>($@"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'INRTests'
                  AND column_name IN ('ProthrombinTime', 'PartialThromboplastinTime', 'INRValue')")
            .ToListAsync();

        // Assert - Verify columns exist (PostgreSQL stores in lowercase but EF maps correctly)
        Assert.Equal(3, columns.Count);
    }

    [Fact]
    public async Task PostgreSQL_NoNVarcharTypes()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>().UseNpgsql(_postgresConnectionString)
            .Options;

        using var context = CreatePostgreSqlContext(options);
        await context.Database.MigrateAsync();

        // Act - Query for any nvarchar type usage (should not exist in PostgreSQL)
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*)::int
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND data_type LIKE '%nvarchar%'";
        
        await context.Database.OpenConnectionAsync();
        var nvarcharCount = (int)(await command.ExecuteScalarAsync() ?? 0);

        // Assert - Should be zero (PostgreSQL uses text/varchar)
        Assert.Equal(0, nvarcharCount);
    }

    [Fact]
    public async Task PostgreSQL_InsertAndQueryUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.PostgreSQL.ApplicationDbContext>().UseNpgsql(_postgresConnectionString)
            .Options;

        using var context = CreatePostgreSqlContext(options);
        await context.Database.MigrateAsync();

        var publicId = Guid.NewGuid();
        var testEmail = $"test-{Guid.NewGuid()}@example.com";
        var user = new BloodThinnerTracker.Shared.Models.User
        {
            PublicId = publicId,
            Name = "Test User",
            Email = testEmail,
            AuthProvider = "Local",
            Role = UserRole.Patient,
            PreferredLanguage = "en-US",
            TimeZone = "UTC",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
        Assert.NotNull(retrievedUser);
        Assert.Equal("Test User", retrievedUser.Name);
        Assert.Equal(testEmail, retrievedUser.Email);
        Assert.True(retrievedUser.Id > 0); // Verify internal Id was generated
        Assert.Equal(publicId, retrievedUser.PublicId);
    }*/

    #region SQL Server Tests

    [Fact]
    public async Task SqlServer_MigrationsApplySuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>().UseSqlServer(_sqlServerConnectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSqlServerContext(options);

        // Act - Apply migrations
        await context.Database.MigrateAsync();

        // Assert - Verify database was created and migrations applied
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect, "Should be able to connect to SQL Server database");

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task SqlServer_AllTablesCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>().UseSqlServer(_sqlServerConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSqlServerContext(options);
        await context.Database.MigrateAsync();

        // Act - Query for expected tables
        var tableNames = await context.Database
            .SqlQuery<string>($@"
                SELECT TABLE_NAME AS Value
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                  AND TABLE_CATALOG = DB_NAME()
                ORDER BY TABLE_NAME")
            .ToListAsync();

        // Assert - All expected tables exist
        var expectedTables = new[] { "AuditLogs", "DataProtectionKeys", "Users", "Medications", "MedicationLogs", "INRTests", "INRSchedules", "RefreshTokens", "__EFMigrationsHistory" };
        foreach (var table in expectedTables)
        {
            Assert.Contains(table, tableNames, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SqlServer_CheckConstraintsWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>().UseSqlServer(_sqlServerConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSqlServerContext(options);
        await context.Database.MigrateAsync();

        // Act & Assert - Create a test user first (PublicId is GUID, but Id is int identity)
        // Note: User is the root tenant entity and does NOT have a UserId column
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO Users (PublicId, Name, Email, AuthProvider, Role, PreferredLanguage, TimeZone, IsActive, EmailVerified, CreatedAt, UpdatedAt, IsDeleted, ReminderAdvanceMinutes, IsEmailNotificationsEnabled, IsPushNotificationsEnabled, IsSmsNotificationsEnabled, TwoFactorEnabled)
            VALUES (NEWID(), 'Test User', 'test@example.com', 'Local', 'Patient', 'en-US', 'UTC', 1, 1, GETUTCDATE(), GETUTCDATE(), 0, 30, 1, 1, 0, 0);
        ");

        // Should throw when violating INR value constraint (< 0.5)
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO INRTests (PublicId, UserId, TestDate, INRValue, Status, IsPointOfCare, ReviewedByProvider, PatientNotified, CreatedAt, UpdatedAt, IsDeleted)
                VALUES (NEWID(), (SELECT TOP 1 Id FROM Users), GETUTCDATE(), 0.3, 0, 0, 0, 0, GETUTCDATE(), GETUTCDATE(), 0);
            ");
        });

        Assert.Contains("CK_INRTest_Value", exception.Message);
    }

    [Fact]
    public async Task SqlServer_UsesNVarcharTypes()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>().UseSqlServer(_sqlServerConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSqlServerContext(options);
        await context.Database.MigrateAsync();

        // Act - Query for nvarchar type usage (SQL Server should use nvarchar)
        var nvarcharColumns = await context.Database
            .SqlQuery<int>($@"
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_CATALOG = DB_NAME()
                  AND DATA_TYPE = 'nvarchar'")
            .FirstOrDefaultAsync();

        // Assert - Should have nvarchar columns (SQL Server default for string types)
        Assert.True(nvarcharColumns > 0, "SQL Server should use nvarchar for string columns");
    }

    [Fact]
    public async Task SqlServer_ColumnNamesAreCaseInsensitive()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>().UseSqlServer(_sqlServerConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSqlServerContext(options);
        await context.Database.MigrateAsync();

        var testPublicId = Guid.NewGuid();
        var testEmail = "case@example.com";

        // Act - Insert with different casing (SQL Server is case-insensitive by default)
        // Note: Id (int) is auto-generated by IDENTITY, PublicId is GUID
        // User is the root tenant entity and does NOT have a UserId column
        await context.Database.ExecuteSqlRawAsync($@"
            INSERT INTO users (PublicId, Name, Email, AuthProvider, Role, PreferredLanguage, TimeZone, IsActive, EmailVerified, CreatedAt, UpdatedAt, IsDeleted, ReminderAdvanceMinutes, IsEmailNotificationsEnabled, IsPushNotificationsEnabled, IsSmsNotificationsEnabled, TwoFactorEnabled)
            VALUES ('{testPublicId}', 'Case Test', '{testEmail}', 'Local', 'Patient', 'en-US', 'UTC', 1, 1, GETUTCDATE(), GETUTCDATE(), 0, 30, 1, 1, 0, 0);
        ");

        // Assert - Should succeed (case-insensitive)
        var user = await context.Users.FirstOrDefaultAsync(u => u.PublicId == testPublicId);
        Assert.NotNull(user);
        Assert.Equal("Case Test", user.Name);
        Assert.Equal(testEmail, user.Email);
    }

    [Fact]
    public async Task SqlServer_InsertAndQueryUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SqlServer.ApplicationDbContext>().UseSqlServer(_sqlServerConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSqlServerContext(options);
        await context.Database.MigrateAsync();

        var publicId = Guid.NewGuid();
        var testEmail = $"test-{Guid.NewGuid()}@example.com";
        var user = new BloodThinnerTracker.Shared.Models.User
        {
            PublicId = publicId,
            Name = "SQL Server Test User",
            Email = testEmail,
            AuthProvider = "Local",
            Role = UserRole.Patient,
            PreferredLanguage = "en-US",
            TimeZone = "UTC",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
        Assert.NotNull(retrievedUser);
        Assert.Equal("SQL Server Test User", retrievedUser.Name);
        Assert.Equal(testEmail, retrievedUser.Email);
        Assert.True(retrievedUser.Id > 0); // Verify internal Id was generated
        Assert.Equal(publicId, retrievedUser.PublicId);
    }

    #endregion

    [Fact]
    public async Task SQLite_MigrationsApplySuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>().UseSqlite("DataSource=:memory:")
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSQLiteContext(options);

        // Act - Create database and apply migrations
        await context.Database.OpenConnectionAsync(); // Keep connection open for in-memory DB
        await context.Database.MigrateAsync();

        // Assert
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect, "Should be able to connect to SQLite database");

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(appliedMigrations);

        await context.Database.CloseConnectionAsync();
    }

    [Fact]
    public async Task SQLite_InsertAndQueryUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BloodThinnerTracker.Data.SQLite.ApplicationDbContext>().UseSqlite("DataSource=:memory:")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        using var context = CreateSQLiteContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.MigrateAsync();

        var publicId = Guid.NewGuid();
        var user = new BloodThinnerTracker.Shared.Models.User
        {
            PublicId = publicId,
            Name = "SQLite Test User",
            Email = $"sqlite-test-{Guid.NewGuid()}@example.com",
            AuthProvider = "Local",
            Role = UserRole.Patient,
            PreferredLanguage = "en-US",
            TimeZone = "UTC",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);
        Assert.NotNull(retrievedUser);
        Assert.Equal("SQLite Test User", retrievedUser.Name);
        Assert.True(retrievedUser.Id > 0); // Verify internal Id was generated
        Assert.Equal(publicId, retrievedUser.PublicId);

        await context.Database.CloseConnectionAsync();
    }
}




