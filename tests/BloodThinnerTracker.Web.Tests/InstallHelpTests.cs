using Bunit;
using Xunit;

namespace BloodThinnerTracker.Web.Tests
{
    public class InstallHelpTests : BunitContext
    {
        [Fact]
        public void InstallHelp_Renders_WhenOpen()
        {
            // Arrange
            var comp = RenderComponent<Shared.InstallHelp>(parameters => parameters.Add(p => p.IsOpen, true));

            // Act
            var markup = comp.Markup;

            // Assert
            Assert.Contains("Install the app", markup);
        }
    }
}
