using Bunit;
using Moq;
using MudBlazor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BloodThinnerTracker.Web.Tests
{
    public class InstallHelpTests : BunitContext
    {
        [Fact]
        public void InstallHelp_Renders_WhenOpen()
        {
            // Arrange
            // Provide minimal stub for ISnackbar which the component injects
            var snackbarMock = new Mock<ISnackbar>();
            Services.AddSingleton<ISnackbar>(snackbarMock.Object);

            // The InstallHelp component is informational and can be rendered
            // directly inside a test host. Use the bUnit Render<T>() API
            // (newer versions expect an Action parameter builder). For a
            // component without parameters we pass an empty builder.
            var comp = Render<Shared.InstallHelp>((System.Action<Bunit.ComponentParameterCollectionBuilder<Shared.InstallHelp>>)(_ => { }));

            // Act
            var markup = comp.Markup;

            // Assert
            Assert.Contains("Install the app", markup);
        }
    }
}
