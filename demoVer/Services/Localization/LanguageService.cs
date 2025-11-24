using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace demoVer.Services.Localization
{
    public class LanguageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LanguageService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCurrentCulture()
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        public void SetCulture(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            var blazorCookieValue = culture.Name;
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(
                "BlazorCulture",
                blazorCookieValue,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
        }

        public IReadOnlyList<(string Code, string Name)> GetAvailableCultures()
        {
            return new List<(string Code, string Name)>
            {
                ("en-US", "English"),
                ("zh-TW", "繁體中文"),
                ("zh-CN", "簡體中文")
            };
        }
    }
}
