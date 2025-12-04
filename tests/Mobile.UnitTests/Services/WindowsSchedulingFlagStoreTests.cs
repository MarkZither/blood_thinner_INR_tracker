using Xunit;

namespace Mobile.UnitTests.Services;

/// <summary>
/// Tests for WindowsSchedulingFlagStore.
/// These tests require Windows-specific APIs (ApplicationData.LocalSettings) and
/// must be run on a Windows device or emulator. They are skipped in CI.
/// </summary>
/// <remarks>
/// The WindowsSchedulingFlagStore implementation uses Windows.Storage.ApplicationData.LocalSettings
/// which is only available in packaged Windows applications. For unit testing, use
/// InMemorySchedulingFlagStore which implements the same ISchedulingFlagStore interface.
///
/// To test WindowsSchedulingFlagStore behavior:
/// 1. Run the MAUI app on Windows
/// 2. Use the Visual Studio Background Task debugger to verify task registration
/// 3. Verify badge updates appear in the taskbar
/// </remarks>
[Trait("Category", "Platform")]
[Trait("Platform", "Windows")]
public class WindowsSchedulingFlagStoreTests
{
    [Fact(Skip = "Requires Windows ApplicationData.LocalSettings - run on device/emulator")]
    public void IsScheduled_DefaultsToFalse()
    {
        // This test requires Windows.Storage.ApplicationData which is not available in unit tests.
        // The behavior is validated via InMemorySchedulingFlagStore tests which use the same interface.
    }

    [Fact(Skip = "Requires Windows ApplicationData.LocalSettings - run on device/emulator")]
    public void SetScheduled_True_PersistsToLocalSettings()
    {
        // This test requires Windows.Storage.ApplicationData which is not available in unit tests.
        // The behavior is validated via InMemorySchedulingFlagStore tests which use the same interface.
    }

    [Fact(Skip = "Requires Windows ApplicationData.LocalSettings - run on device/emulator")]
    public void SetScheduled_False_ClearsLocalSettingsValue()
    {
        // This test requires Windows.Storage.ApplicationData which is not available in unit tests.
        // The behavior is validated via InMemorySchedulingFlagStore tests which use the same interface.
    }
}
