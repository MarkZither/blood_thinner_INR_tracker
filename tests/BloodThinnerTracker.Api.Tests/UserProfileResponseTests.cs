using System.Text.Json;
using AutoBogus;
using Xunit;
using BloodThinnerTracker.Api.Controllers;

namespace BloodThinnerTracker.Api.Tests
{
    public class UserProfileResponseTests
    {
        private readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        [Fact]
        public void UserProfileResponse_AutoFaker_RoundTrip_Valid()
        {
            var faker = new AutoFaker<UserProfileResponse>()
                .RuleFor(x => x.PublicId, f => f.Random.Guid())
                .RuleFor(x => x.Name, f => f.Person.FullName)
                .RuleFor(x => x.Email, f => f.Internet.Email());

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.PublicId);
            Assert.Contains("@", dto.Email);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<UserProfileResponse>(json, _opts);
            Assert.Equal(dto.PublicId, dto2.PublicId);
            Assert.Equal(dto.Email, dto2.Email);
        }
    }
}
