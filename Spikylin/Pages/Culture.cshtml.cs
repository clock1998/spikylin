using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Spikylin.Pages
{
    public class CultureModel : PageModel
    {
        public IActionResult OnGetSet(string culture, string returnUrl)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions
                    {
                        Path = "/", // Make the cookie available for the whole application
                        Expires = DateTimeOffset.UtcNow.AddYears(1), // Cookie persistence
                        IsEssential = true, // Mark as essential if you use GDPR cookie consent
                        SameSite = SameSiteMode.Lax // Good security practice
                    }
                );
            }

            // Validate the returnUrl to prevent open redirect attacks
            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Fallback to the home page if returnUrl is invalid or not provided
            return LocalRedirect("/");
        }
    }
}
