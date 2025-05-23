using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;


namespace Spikylin.Pages.Shared.Components.CultureSwitcher
{
    public class CultureSwitcherViewComponent : ViewComponent
    {
        private readonly IOptions<RequestLocalizationOptions> _localizationOptions;
        public CultureSwitcherViewComponent(IOptions<RequestLocalizationOptions> localizationOptions) =>
            _localizationOptions = localizationOptions;

        public IViewComponentResult Invoke()
        {
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var currentUICulture = cultureFeature?.RequestCulture.UICulture ?? CultureInfo.CurrentUICulture; // Fallback
            var supportedCultures = _localizationOptions.Value.SupportedUICultures?.ToList() ?? new List<CultureInfo>();
            var model = new CultureSwitcherModel
            {
                CurrentUICulture = currentUICulture,
                SupportedCultures = supportedCultures
            };
            return View(model);
        }
    }
}
