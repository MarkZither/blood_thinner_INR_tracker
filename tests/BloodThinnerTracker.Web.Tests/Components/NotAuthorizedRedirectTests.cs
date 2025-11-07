using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using System.Linq;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BloodThinnerTracker.Web.Tests.Components
{
    public class NotAuthorizedRedirectTests : TestContext
    {
        private readonly TestAuthorizationContext _authContext;

        public NotAuthorizedRedirectTests()
        {
            // Register TestAuthorization and NavigationManager
            // Create and register TestAuthorization which wires up policy provider and other auth services
            var authCtx = this.AddTestAuthorization();
            _authContext = authCtx;

            // Provide a TestNavigationManager (bUnit FakeNavigationManager) so we can assert navigations
            Services.AddSingleton<NavigationManager>(new Bunit.TestDoubles.FakeNavigationManager(this));

            // Provide minimal stubs for services the pages expect using Moq
            var snackbarMock = new Mock<ISnackbar>();
            Services.AddSingleton(snackbarMock.Object);

            var dialogMock = new Mock<IDialogService>();
            Services.AddSingleton(dialogMock.Object);

            // HttpClient used by pages can be a default one (won't be called in NotAuthorized)
            Services.AddSingleton(new HttpClient { BaseAddress = new System.Uri("https://localhost/") });
        }

        [Theory]
        [InlineData("/inr", typeof(BloodThinnerTracker.Web.Components.Pages.INRTracking))]
        [InlineData("/medications", typeof(BloodThinnerTracker.Web.Components.Pages.Medications))]
        [InlineData("/profile", typeof(BloodThinnerTracker.Web.Components.Pages.Profile))]
        [InlineData("/", typeof(BloodThinnerTracker.Web.Components.Pages.Dashboard))]
        public void NotAuthorized_RedirectsToLogin_WithEncodedRelativeReturnUrl(string initialUri, System.Type componentType)
        {
            // Arrange: start from an absolute URI that would normally be the current page
            var nav = (Bunit.TestDoubles.FakeNavigationManager)Services.GetRequiredService<NavigationManager>();
            nav.NavigateTo("https://localhost" + initialUri);

            // Ensure the authorization state is NotAuthorized
            _authContext.SetNotAuthorized();

            // Act: render the page by type using generic RenderComponent via reflection
            var comp = RenderComponentByType(componentType);
            // Assert: navigation was performed to /login with encoded relative returnUrl
            Assert.Contains("/login?returnUrl=", nav.Uri);

            // Extract encoded returnUrl and ensure it equals the encoded relative path
            var uri = new System.Uri(nav.Uri);
            var qp = QueryHelpers.ParseQuery(uri.Query);
            qp.TryGetValue("returnUrl", out var encodedValues);
            var encoded = encodedValues.FirstOrDefault();
            Assert.False(string.IsNullOrEmpty(encoded));

            var decoded = System.Uri.UnescapeDataString(encoded);
            // decoded should be a path starting with '/'
            Assert.StartsWith("/", decoded);

            // The decoded relative path should equal the original initialUri (normalized)
            // Normalize: ensure leading slash
            var expected = initialUri.StartsWith("/") ? initialUri : "/" + initialUri;
            Assert.Equal(expected, decoded);
        }

        private IRenderedFragment RenderComponentByType(System.Type componentType)
        {
            // Find the generic RenderComponent<T>() method on TestContext
            var method = typeof(Bunit.TestContext)
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "RenderComponent" && m.IsGenericMethod && m.GetGenericArguments().Length == 1);

            if (method == null)
                throw new System.InvalidOperationException("RenderComponent<T>() reflection helper could not find the method.");

            var generic = method.MakeGenericMethod(componentType);
            var componentParameterType = typeof(Bunit.ComponentParameter);
            var emptyParams = System.Array.CreateInstance(componentParameterType, 0);
            var rendered = generic.Invoke(this, new object[] { emptyParams }) as IRenderedFragment;
            if (rendered == null)
                throw new System.InvalidOperationException($"Failed to render component of type {componentType.FullName}.");

            return rendered;
        }


    }

    // No-op helpers removed; we use Moq to provide required services in the test setup.
}
