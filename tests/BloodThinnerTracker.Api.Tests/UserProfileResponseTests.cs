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
                .RuleFor(x => x.Id, f => f.Random.Guid())
                .RuleFor(x => x.FirstName, f => f.Person.FirstName)
                .RuleFor(x => x.LastName, f => f.Person.LastName)
                .RuleFor(x => x.Email, f => f.Internet.Email());

            var dto = faker.Generate();
            Assert.NotEqual(default, dto.Id);
            Assert.Contains("@", dto.Email);

            var json = JsonSerializer.Serialize(dto, _opts);
            var dto2 = JsonSerializer.Deserialize<UserProfileResponse>(json, _opts);
            Assert.NotNull(dto2);
            Assert.Equal(dto.Id, dto2!.Id);
            Assert.Equal(dto.Email, dto2.Email);
        }
    }
}
