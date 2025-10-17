using BloodThinnerTracker.Mobile.Services;
using BloodThinnerTracker.Shared.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BloodThinnerTracker.Mobile.ViewModels;

/// <summary>
/// ViewModel for the main dashboard showing key metrics and recent activity
/// </summary>
public class DashboardViewModel : BaseViewModel
{
    private readonly IMedicalDataService _medicalDataService;
    private readonly INotificationService _notificationService;
    private readonly ISyncService _syncService;

    private string _welcomeMessage = string.Empty;
    private DateTime _lastSyncTime;
    private bool _hasRecentINR;
    private double _latestINRValue;
    private string _inrStatus = string.Empty;
    private int _todayMedicationCount;
    private int _takenMedicationCount;
    private int _upcomingReminders;

    public DashboardViewModel(
        IMedicalDataService medicalDataService,
        INotificationService notificationService,
        ISyncService syncService)
    {
        _medicalDataService = medicalDataService;
        _notificationService = notificationService;
        _syncService = syncService;

        Title = "Dashboard";
        
        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
        LogMedicationCommand = new AsyncRelayCommand(NavigateToLogMedicationAsync);
        AddINRTestCommand = new AsyncRelayCommand(NavigateToAddINRTestAsync);
        ViewINRTrendsCommand = new AsyncRelayCommand(NavigateToINRTrendsAsync);
        SyncDataCommand = new AsyncRelayCommand(SyncDataAsync);

        // Initialize collections
        RecentMedicationLogs = new ObservableCollection<MedicationLog>();
        RecentINRTests = new ObservableCollection<INRTest>();
        TodayReminders = new ObservableCollection<MedicationReminder>();

        // Load initial data
        _ = Task.Run(InitializeAsync);
    }

    // Properties
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    public DateTime LastSyncTime
    {
        get => _lastSyncTime;
        set => SetProperty(ref _lastSyncTime, value);
    }

    public bool HasRecentINR
    {
        get => _hasRecentINR;
        set => SetProperty(ref _hasRecentINR, value);
    }

    public double LatestINRValue
    {
        get => _latestINRValue;
        set => SetProperty(ref _latestINRValue, value);
    }

    public string INRStatus
    {
        get => _inrStatus;
        set => SetProperty(ref _inrStatus, value);
    }

    public int TodayMedicationCount
    {
        get => _todayMedicationCount;
        set => SetProperty(ref _todayMedicationCount, value);
    }

    public int TakenMedicationCount
    {
        get => _takenMedicationCount;
        set => SetProperty(ref _takenMedicationCount, value);
    }

    public int UpcomingReminders
    {
        get => _upcomingReminders;
        set => SetProperty(ref _upcomingReminders, value);
    }

    public double MedicationAdherencePercentage => 
        TodayMedicationCount > 0 ? ((double)TakenMedicationCount / TodayMedicationCount) * 100 : 0;

    // Collections
    public ObservableCollection<MedicationLog> RecentMedicationLogs { get; }
    public ObservableCollection<INRTest> RecentINRTests { get; }
    public ObservableCollection<MedicationReminder> TodayReminders { get; }

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand LogMedicationCommand { get; }
    public ICommand AddINRTestCommand { get; }
    public ICommand ViewINRTrendsCommand { get; }
    public ICommand SyncDataCommand { get; }

    private async Task InitializeAsync()
    {
        SetWelcomeMessage();
        await RefreshDataAsync();
    }

    private void SetWelcomeMessage()
    {
        var hour = DateTime.Now.Hour;
        var greeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        // In a real app, you'd get the user's name from the auth service
        WelcomeMessage = $"{greeting}, John!";
    }

    private async Task RefreshDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load recent medication logs (last 7 days)
            var fromDate = DateTime.Today.AddDays(-7);
            var medicationLogs = await _medicalDataService.GetMedicationLogsAsync(fromDate);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecentMedicationLogs.Clear();
                if (medicationLogs?.Any() == true)
                {
                    foreach (var log in medicationLogs.Take(5))
                    {
                        RecentMedicationLogs.Add(log);
                    }
                }
            });

            // Load recent INR tests (last 30 days)
            fromDate = DateTime.Today.AddDays(-30);
            var inrTests = await _medicalDataService.GetINRTestsAsync(fromDate);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecentINRTests.Clear();
                if (inrTests?.Any() == true)
                {
                    foreach (var test in inrTests.Take(3))
                    {
                        RecentINRTests.Add(test);
                    }

                    // Update latest INR status
                    var latestTest = inrTests.OrderByDescending(t => t.TestDate).First();
                    LatestINRValue = (double)latestTest.INRValue;
                    HasRecentINR = true;
                    UpdateINRStatus(latestTest.INRValue, latestTest.TargetMinINR, latestTest.TargetMaxINR);
                }
                else
                {
                    HasRecentINR = false;
                    INRStatus = "No recent tests";
                }
            });

            // Calculate today's medication statistics
            var today = DateTime.Today;
            var todayLogs = medicationLogs?.Where(l => l.ActualTakenAt?.Date == today).ToList() ?? new List<MedicationLog>();
            
            // Get today's medications (this would come from scheduled medications)
            var medications = await _medicalDataService.GetMedicationsAsync();
            var todayMedications = medications?.Where(m => m.IsActive).ToList() ?? new List<Medication>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TodayMedicationCount = todayMedications.Count;
                TakenMedicationCount = todayLogs.Count(l => l.Status == MedicationLogStatus.Taken);
                UpcomingReminders = Math.Max(0, TodayMedicationCount - TakenMedicationCount);
                
                // Update medication adherence property
                OnPropertyChanged(nameof(MedicationAdherencePercentage));
            });

            LastSyncTime = DateTime.Now;
        });
    }

    private void UpdateINRStatus(decimal inrValue, decimal? targetMin, decimal? targetMax)
    {
        if (!targetMin.HasValue || !targetMax.HasValue)
        {
            INRStatus = "Target range not set";
            return;
        }

        if (inrValue < targetMin)
        {
            INRStatus = "Below target range";
        }
        else if (inrValue > targetMax)
        {
            INRStatus = "Above target range";
        }
        else
        {
            INRStatus = "In target range";
        }
    }

    private async Task NavigateToLogMedicationAsync()
    {
        await Shell.Current.GoToAsync("//medication");
    }

    private async Task NavigateToAddINRTestAsync()
    {
        await Shell.Current.GoToAsync("inr/add");
    }

    private async Task NavigateToINRTrendsAsync()
    {
        await Shell.Current.GoToAsync("//inr");
    }

    private async Task SyncDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            var success = await _syncService.SyncAllDataAsync();
            if (success)
            {
                await RefreshDataAsync();
                await _notificationService.ShowImmediateNotificationAsync(
                    "Sync Complete", 
                    "Your medical data has been synchronized.", 
                    NotificationType.Success);
            }
            else
            {
                await _notificationService.ShowImmediateNotificationAsync(
                    "Sync Failed", 
                    "Unable to sync data. Please check your connection.", 
                    NotificationType.Error);
            }
        });
    }
}

// Helper models for the dashboard
public class MedicationReminder
{
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public bool IsTaken { get; set; }
    public bool IsOverdue => DateTime.Now > ScheduledTime && !IsTaken;
}