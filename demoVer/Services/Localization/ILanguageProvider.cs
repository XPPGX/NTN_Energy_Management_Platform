using System.Threading.Tasks;

namespace demoVer.Services.Localization
{
    public interface ILanguageProvider
    {
        string GetCurrentCulture();
        Task SetCultureAsync(string culture);
        string GetTranslation(string key);
    }
}
