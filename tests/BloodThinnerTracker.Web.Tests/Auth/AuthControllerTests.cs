using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Google;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Web.Controllers;
using BloodThinnerTracker.Web.Services;

namespace BloodThinnerTracker.Web.Tests.Auth
{
    public class AuthControllerTests
    {
        private AuthController CreateController()
        {
            var logger = Mock.Of<ILogger<AuthController>>();

            // CustomAuthenticationStateProvider requires constructor args; create a mock with updated constructor
            var mockCache = new Mock<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var mockHttp = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var mockLoggerProvider = new Mock<ILogger<CustomAuthenticationStateProvider>>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var authStateProvider = new Mock<CustomAuthenticationStateProvider>(
                mockCache.Object, 
                mockHttp.Object, 
                mockLoggerProvider.Object,
                mockHttpClientFactory.Object,
                mockConfiguration.Object).Object;

            var controller = new AuthController(logger, authStateProvider);

            // Provide a minimal ControllerContext and Url helper so Url.Content works in unit test
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext(actionContext);
            controller.Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(actionContext);

            return controller;
        }

        [Fact]
        public void LoginMicrosoft_Should_Set_RedirectUri_And_Items_ReturnUrl()
        {
            // Arrange
            var controller = CreateController();

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

        [Fact]
        public void LoginMicrosoft_NullReturnUrl_ShouldDefaultToDashboard()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginMicrosoft(null);

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/dashboard", challenge.Properties.Items["returnUrl"]);
        }

        [Fact]
        public void LoginMicrosoft_EmptyReturnUrl_ShouldDefaultToDashboard()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginMicrosoft("");

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/dashboard", challenge.Properties.Items["returnUrl"]);
        }

        [Theory]
        [InlineData("https://evil.com/attack")]
        [InlineData("//evil.com/attack")]
        [InlineData("javascript:alert('xss')")]
        [InlineData("data:text/html,<script>alert('xss')</script>")]
        [InlineData("%252F%252Fevil.com")] // Double-encoded protocol-relative URL
        public void LoginMicrosoft_MaliciousReturnUrl_ShouldDefaultToDashboard(string maliciousUrl)
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginMicrosoft(maliciousUrl);

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/dashboard", challenge.Properties.Items["returnUrl"]);
        }

        [Theory]
        [InlineData("/dashboard")]
        [InlineData("/medications/123")]
        [InlineData("/inr-tests")]
        [InlineData("/profile")]
        public void LoginMicrosoft_ValidLocalPath_ShouldUseProvidedPath(string validPath)
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginMicrosoft(validPath);

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal(validPath, challenge.Properties.Items["returnUrl"]);
        }

        [Fact]
        public void LoginGoogle_ValidReturnUrl_ShouldSetCorrectProperties()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginGoogle("/medications/456");

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.Contains("/oauth-complete?returnUrl=", challenge.Properties.RedirectUri);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/medications/456", challenge.Properties.Items["returnUrl"]);
            Assert.Equal(GoogleDefaults.AuthenticationScheme, challenge.AuthenticationSchemes?[0]);
        }

        [Fact]
        public void LoginGoogle_NullReturnUrl_ShouldDefaultToDashboard()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginGoogle(null);

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/dashboard", challenge.Properties.Items["returnUrl"]);
        }

        [Theory]
        [InlineData("https://evil.com/attack")]
        [InlineData("//evil.com/attack")]
        [InlineData("javascript:alert('xss')")]
        public void LoginGoogle_MaliciousReturnUrl_ShouldDefaultToDashboard(string maliciousUrl)
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.LoginGoogle(maliciousUrl);

            // Assert
            var challenge = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challenge.Properties);
            Assert.True(challenge.Properties.Items.ContainsKey("returnUrl"));
            Assert.Equal("/dashboard", challenge.Properties.Items["returnUrl"]);
        }
    }
}
