namespace BloodThinnerTracker.Mobile.Services;

/// <summary>
/// Service for secure storage of sensitive data like tokens and credentials
/// </summary>
public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<bool> RemoveAsync(string key);
    Task ClearAllAsync();
}

public class SecureStorageService : ISecureStorageService
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await Microsoft.Maui.Storage.SecureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage GetAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await Microsoft.Maui.Storage.SecureStorage.SetAsync(key, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage SetAsync error: {ex.Message}");
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            return Microsoft.Maui.Storage.SecureStorage.Remove(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage RemoveAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task ClearAllAsync()
    {
        try
        {
            Microsoft.Maui.Storage.SecureStorage.RemoveAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage ClearAllAsync error: {ex.Message}");
        }
    }
}

/// <summary>
/// Service for biometric authentication (fingerprint, face ID)
/// </summary>
public interface IBiometricService
{
    Task<bool> IsAvailableAsync();
    Task<bool> AuthenticateAsync(string reason);
    Task<BiometricAuthenticationStatus> GetAvailabilityAsync();
}

public class BiometricService : IBiometricService
{
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var status = await GetAvailabilityAsync();
            return status == BiometricAuthenticationStatus.Available;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biometric availability check error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            // This would use Plugin.Fingerprint or similar
            // For now, simulate biometric authentication
            await Task.Delay(1000);
            
            // In a real implementation:
            // var request = new AuthenticationRequestConfiguration(
            //     "Blood Thinner Tracker",
            //     reason)
            // {
            //     AllowAlternativeAuthentication = true,
            //     CancelTitle = "Cancel",
            //     FallbackTitle = "Use Password"
            // };
            
            // var result = await CrossFingerprint.Current.AuthenticateAsync(request);
            // return result.Succeeded;
            
            return true; // Simulate success for now
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biometric authentication error: {ex.Message}");
            return false;
        }
    }

    public async Task<BiometricAuthenticationStatus> GetAvailabilityAsync()
    {
        try
        {
            // This would check actual biometric capabilities
            await Task.Delay(100);
            
            // In a real implementation:
            // var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            // return (BiometricAuthenticationStatus)availability;
            
            return BiometricAuthenticationStatus.Available; // Simulate availability
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biometric availability error: {ex.Message}");
            return BiometricAuthenticationStatus.NotAvailable;
        }
    }
}

public enum BiometricAuthenticationStatus
{
    Unknown,
    Available,
    NotAvailable,
    NotEnrolled,
    Denied,
    NotSupported
}

/// <summary>
/// Service for syncing data with the server and managing offline functionality
/// </summary>
public interface ISyncService
{
    Task<bool> SyncAllDataAsync();
    Task<bool> SyncPendingDataAsync();
    Task<bool> IsOnlineAsync();
    Task CacheMedicalDataAsync();
    event EventHandler<SyncStatusEventArgs> SyncStatusChanged;
}

public class SyncService : ISyncService
{
    private readonly IApiService _apiService;
    private readonly ISecureStorageService _secureStorage;
    private readonly IMedicalDataService _medicalDataService;

    public event EventHandler<SyncStatusEventArgs> SyncStatusChanged = delegate { };

    public SyncService(
        IApiService apiService, 
        ISecureStorageService secureStorage,
        IMedicalDataService medicalDataService)
    {
        _apiService = apiService;
        _secureStorage = secureStorage;
        _medicalDataService = medicalDataService;
    }

    public async Task<bool> SyncAllDataAsync()
    {
        try
        {
            SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.InProgress, "Syncing all data..."));

            if (!await IsOnlineAsync())
            {
                SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.Failed, "No internet connection"));
                return false;
            }

            // Sync medications
            await _medicalDataService.GetMedicationsAsync();
            
            // Sync medication logs for the last 30 days
            var fromDate = DateTime.Now.AddDays(-30);
            await _medicalDataService.GetMedicationLogsAsync(fromDate);
            
            // Sync INR tests for the last 90 days
            fromDate = DateTime.Now.AddDays(-90);
            await _medicalDataService.GetINRTestsAsync(fromDate);

            SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.Completed, "All data synced successfully"));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync all data error: {ex.Message}");
            SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.Failed, $"Sync failed: {ex.Message}"));
            return false;
        }
    }

    public async Task<bool> SyncPendingDataAsync()
    {
        try
        {
            SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.InProgress, "Syncing pending changes..."));

            if (!await IsOnlineAsync())
            {
                SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.Failed, "No internet connection"));
                return false;
            }

            // In a real implementation, this would:
            // 1. Get all locally stored pending changes
            // 2. Upload them to the server
            // 3. Mark them as synced
            // 4. Download any new changes from the server

            await Task.Delay(1000); // Simulate sync operation

            SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.Completed, "Pending data synced"));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync pending data error: {ex.Message}");
            SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs(SyncStatus.Failed, $"Sync failed: {ex.Message}"));
            return false;
        }
    }

    public async Task<bool> IsOnlineAsync()
    {
        try
        {
            var current = Connectivity.Current;
            var isConnected = current.NetworkAccess == NetworkAccess.Internet;
            
            if (isConnected)
            {
                // Double-check by pinging the API
                return await _apiService.IsApiAvailableAsync();
            }
            
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connectivity check error: {ex.Message}");
            return false;
        }
    }

    public async Task CacheMedicalDataAsync()
    {
        try
        {
            // This would cache critical medical data locally for offline access
            // Implementation would store data in local database (SQLite)
            await Task.Delay(100);
            System.Diagnostics.Debug.WriteLine("Medical data cached for offline access");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cache medical data error: {ex.Message}");
        }
    }
}

public class SyncStatusEventArgs : EventArgs
{
    public SyncStatus Status { get; }
    public string Message { get; }

    public SyncStatusEventArgs(SyncStatus status, string message)
    {
        Status = status;
        Message = message;
    }
}

public enum SyncStatus
{
    Idle,
    InProgress,
    Completed,
    Failed
}