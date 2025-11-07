using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using BloodThinnerTracker.Web.Services;

namespace BloodThinnerTracker.Web.Pages.Account;

public partial class Login
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<Login> Logger { get; set; } = default!;

    private async Task OnLoginSucceeded()
    {
        // Read returnUrl query parameter if present
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var qs = QueryHelpers.ParseQuery(uri.Query);
        if (qs.TryGetValue("returnUrl", out var returnValues))
        {
            var raw = returnValues.FirstOrDefault();
            var res = ReturnUrlValidator.Validate(raw);
            if (res.IsValid && res.Normalized is not null)
            {
                Navigation.NavigateTo(res.Normalized);
                return;
            }
            Logger.LogError("ReturnUrlBlocked: {RawReturnUrl} - {Validation}", raw, res.ValidationResultCode);
        }

        // Fallback
        Navigation.NavigateTo("/dashboard");
    }
}
