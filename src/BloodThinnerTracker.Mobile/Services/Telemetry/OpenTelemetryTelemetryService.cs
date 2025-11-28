using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BloodThinnerTracker.Mobile.Services.Telemetry
{
    /// <summary>
    /// Telemetry service backed by OpenTelemetry APIs (ActivitySource + Meter).
    /// This delegates events to an ActivitySource and metrics to a Meter counter.
    /// Exporters are configured via DI in MauiProgram.
    /// </summary>
    public class OpenTelemetryTelemetryService : ITelemetryService, IDisposable
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("BloodThinnerTracker.Mobile");
        private static readonly Meter Meter = new Meter("BloodThinnerTracker.Mobile.Metrics");
        private readonly ConcurrentDictionary<string, Counter<double>> _counters = new();
        private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();
        private bool _disposed;

        public void TrackEvent(string name, IDictionary<string, string>? properties = null)
        {
            // Start a brief activity to attach properties as tags
            using var a = ActivitySource.StartActivity(name, ActivityKind.Internal);
            if (a != null && properties != null)
            {
                foreach (var kv in properties)
                {
                    try { a.SetTag(kv.Key, kv.Value); } catch { }
                }
            }
            // Add an explicit Event as well
            try
            {
                a?.AddEvent(new ActivityEvent(name));
            }
            catch { }
        }

        public void TrackMetric(string name, double value)
        {
            try
            {
                var counter = _counters.GetOrAdd(name, n => Meter.CreateCounter<double>(n));
                counter.Add(value);
            }
            catch { }
        }

        public void TrackHistogram(string name, double value)
        {
            try
            {
                var hist = _histograms.GetOrAdd(name, n => Meter.CreateHistogram<double>(n));
                hist.Record(value);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { Meter.Dispose(); } catch { }
            try { ActivitySource.Dispose(); } catch { }
        }
    }
}
