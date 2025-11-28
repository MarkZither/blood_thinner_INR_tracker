using System.Collections.Generic;

namespace BloodThinnerTracker.Mobile.Services.Telemetry
{
    public interface ITelemetryService
    {
        void TrackMetric(string name, double value);
        void TrackHistogram(string name, double value);
        void TrackEvent(string name, IDictionary<string, string>? properties = null);
    }
}
