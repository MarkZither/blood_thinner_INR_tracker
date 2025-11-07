using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Web.Controllers;
using BloodThinnerTracker.Web.Services;

namespace BloodThinnerTracker.Web.Tests.Auth
{
    public class AuthControllerTests
    {
        [Fact]
        public void LoginMicrosoft_Should_Set_RedirectUri_And_Items_ReturnUrl()
        {
            // Arrange
            var logger = Mock.Of<ILogger<AuthController>>();

            // CustomAuthenticationStateProvider requires constructor args; create a mock with constructor parameters
            var mockCache = new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var mockHttp = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var mockLoggerProvider = new Mock<ILogger<CustomAuthenticationStateProvider>>();
            var authStateProvider = new Mock<CustomAuthenticationStateProvider>(mockCache.Object, mockHttp.Object, mockLoggerProvider.Object).Object;

            var controller = new AuthController(logger, authStateProvider);

            // Provide a minimal ControllerContext and Url helper so Url.Content works in unit test
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext(actionContext);
            controller.Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(actionContext);

            // Act
            var result = controller.LoginMicrosoft("/medications/123");

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            // AuthenticationProperties are in the AuthenticationProperties of the ChallengeResult
            Assert.NotNull(challenge.Properties);
            Assert.Contains("/oauth-complete?returnUrl=", challenge.Properties.RedirectUri);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/medications/123", challenge.Properties.Items["returnUrl"]);
            Assert.Equal(MicrosoftAccountDefaults.AuthenticationScheme, challenge.AuthenticationSchemes?[0]);
        }
    }
}
