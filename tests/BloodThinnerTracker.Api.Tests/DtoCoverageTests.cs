using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Xunit;
using BloodThinnerTracker.Shared.Models;
using BloodThinnerTracker.Api.Controllers;

#nullable disable

namespace BloodThinnerTracker.Api.Tests
{
    public class DtoCoverageTests
    {
        private readonly JsonSerializerOptions _opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        [Fact]
        public void RequestAndResponseDtos_SerializeRoundTrip_AllPublicProperties()
        {
            var types = new List<Type> {
                // Shared models
                typeof(CreateDosagePatternRequest),
                typeof(CreateINRTestRequest),
                typeof(UpdateINRTestRequest),
                typeof(Medication),
                typeof(MedicationDosagePattern),
                typeof(MedicationLog),
                typeof(PatternSummary),
                typeof(ScheduleEntry),
                typeof(ScheduleSummary),
                typeof(INRTest),
                typeof(INRTestResponse),

                // API controller DTOs
                typeof(CreateMedicationRequest),
                typeof(DeactivateAccountRequest),
                typeof(DeactivateMedicationRequest),
                typeof(LogMedicationRequest),
                typeof(MedicationResponse),
                typeof(UpdateMedicationLogRequest),
                typeof(UpdateMedicationRequest),
                typeof(UpdateNotificationPreferencesRequest),
                typeof(UpdateUserProfileRequest),
                typeof(UserProfileResponse),
                typeof(ValidationResult)
            };

            foreach (var t in types)
            {
                var instance = Activator.CreateInstance(t);
                Assert.NotNull(instance);
                Populate(instance);

                var json = JsonSerializer.Serialize(instance, _opts);
                var obj2 = JsonSerializer.Deserialize(json, t, _opts);
                Assert.NotNull(obj2);
                var json2 = JsonSerializer.Serialize(obj2, _opts);

                Assert.Equal(json, json2);
            }
        }

        private void Populate(object obj)
        {
            var t = obj.GetType();
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!p.CanWrite) continue;
                var pt = p.PropertyType;
                try
                {
                    var val = GetSampleValue(pt);
                    if (val != null)
                        p.SetValue(obj, val);
                }
                catch
                {
                    // best-effort: skip properties we can't set
                }
            }
        }

        private object GetSampleValue(Type pt)
        {
            if (pt == typeof(string)) return "sample";
            if (pt == typeof(int) || pt == typeof(int?)) return 1;
            if (pt == typeof(long) || pt == typeof(long?)) return 1L;
            if (pt == typeof(decimal) || pt == typeof(decimal?)) return 1.23m;
            if (pt == typeof(double) || pt == typeof(double?)) return 1.23;
            if (pt == typeof(bool) || pt == typeof(bool?)) return true;
            if (pt == typeof(Guid) || pt == typeof(Guid?)) return Guid.NewGuid();
            if (pt == typeof(DateTime) || pt == typeof(DateTime?)) return DateTime.UtcNow;

            if (pt.IsEnum) return Enum.GetValues(pt).Length > 0 ? Enum.GetValues(pt).GetValue(0) : Activator.CreateInstance(pt);

            if (pt.IsArray)
            {
                var elem = pt.GetElementType();
                return Array.CreateInstance(elem, 0);
            }

            if (pt.IsGenericType)
            {
                var gt = pt.GetGenericTypeDefinition();
                if (gt == typeof(List<>) || gt == typeof(IEnumerable<>) || gt == typeof(ICollection<>) || gt == typeof(IList<>))
                {
                    var elem = pt.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elem);
                    return Activator.CreateInstance(listType);
                }

                if (gt == typeof(Dictionary<,>))
                {
                    var args = pt.GetGenericArguments();
                    return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args[0], args[1]));
                }
            }

            try
            {
                return Activator.CreateInstance(pt);
            }
            catch
            {
                return null;
            }
        }
    }
}
