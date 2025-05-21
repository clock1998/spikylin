using System.Globalization;

namespace Spikylin.Pages.Shared.Components.CultureSwitcher
{
    public class CultureSwitcherModel
    {
        public CultureInfo CurrentUICulture { get; set; } = null!;
        public List<CultureInfo> SupportedCultures { get; set; } = null!;
    }
}
