using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using demoVer.Resources;

namespace demoVer.Services.Localization
{
    public class ResourceLanguageProvider : ILanguageProvider
    {
        private readonly NavigationManager _navigationManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJSRuntime _jsRuntime;

        public ResourceLanguageProvider(
            NavigationManager navigationManager,
            IStringLocalizer<SharedResource> localizer,
            IHttpContextAccessor httpContextAccessor,
            IJSRuntime jsRuntime)
        {
            _navigationManager = navigationManager;
            _localizer = localizer;
            _httpContextAccessor = httpContextAccessor;
            _jsRuntime = jsRuntime;
        }

        public string GetCurrentCulture()
        {
            var absoluteUri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
            var queryParameters = QueryHelpers.ParseQuery(absoluteUri.Query);
            var cultureFromQuery = queryParameters.GetValueOrDefault("culture").ToString();

            if (!string.IsNullOrEmpty(cultureFromQuery))
            {
                return cultureFromQuery;
            }

            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var cookieCulture = context.Request.Cookies["BlazorCulture"];
                if (!string.IsNullOrWhiteSpace(cookieCulture))
                {
                    return cookieCulture;
                }

                var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                if (requestCultureFeature != null)
                {
                    return requestCultureFeature.RequestCulture.UICulture.Name;
                }
            }

            return "en-US";
        }

        public async Task SetCultureAsync(string culture)
        {
            await _jsRuntime.InvokeVoidAsync("setCultureCookie", culture);
            var targetUri = _navigationManager.GetUriWithQueryParameter("culture", culture);
            _navigationManager.NavigateTo(targetUri, forceLoad: true);
        }

        public string GetTranslation(string key)
        {
            var localizedValue = _localizer[key];
            return localizedValue.ResourceNotFound ? key : localizedValue.Value;
        }
    }
}
