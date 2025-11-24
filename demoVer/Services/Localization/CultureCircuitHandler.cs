using System.Globalization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;

namespace demoVer.Services.Localization
{
    public sealed class CultureCircuitHandler : CircuitHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private CultureInfo? _culture;
        private CultureInfo? _uiCulture;

        public CultureCircuitHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                var cultureFeature = httpContext.Features.Get<IRequestCultureFeature>();
                if (cultureFeature != null)
                {
                    _culture = cultureFeature.RequestCulture.Culture;
                    _uiCulture = cultureFeature.RequestCulture.UICulture;

                    CultureInfo.DefaultThreadCurrentCulture = _culture;
                    CultureInfo.DefaultThreadCurrentUICulture = _uiCulture;
                }
            }

            return base.OnConnectionUpAsync(circuit, cancellationToken);
        }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            if (_culture != null && _uiCulture != null)
            {
                CultureInfo.DefaultThreadCurrentCulture = _culture;
                CultureInfo.DefaultThreadCurrentUICulture = _uiCulture;
            }

            return base.OnCircuitOpenedAsync(circuit, cancellationToken);
        }
    }
}
