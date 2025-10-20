using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace demoVer.Utils
{
    public class ThemeLoader
    {
        public string cssFolderPath = string.Empty;

        public async Task ApplyTheme(IJSRuntime JS)
        {
            //1. 設置不同環境下的 CSS 路徑
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cssFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css");
                return;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                cssFolderPath = "/userdata/CMU3/Docs/css";

                if (!Directory.Exists(cssFolderPath))
                {
                    Directory.CreateDirectory(cssFolderPath);
                    Console.WriteLine($"[ThemeLoader][ApplyTheme] Created directory at {cssFolderPath}");
                }
                else
                {
                    Console.WriteLine($"[ThemeLoader][ApplyTheme] Directory already exists at {cssFolderPath}"); 
                }
            }

            //2. Load the CSS files
            await LoadCssFiles(JS);
        }

        private async Task LoadCssFiles(IJSRuntime JS)
        {
            string[] cssFiles = Directory.GetFiles(cssFolderPath, "*.css", SearchOption.TopDirectoryOnly);
            if (cssFiles.Length == 0)
            {
                Console.WriteLine($"[ThemeLoader][LoadCssFiles] No CSS files found in {cssFolderPath}");
                return;
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var file in cssFiles)
                {
                    try
                    {
                        string cssContent = File.ReadAllText(file);
                        sb.AppendLine($"/* {file} */");
                        sb.AppendLine(cssContent);
                        sb.AppendLine();
                        Console.WriteLine($"[ThemeLoader][LoadCssFiles] Loaded CSS file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ThemeLoader][LoadCssFiles] Error loading CSS file {file}: {ex.Message}");
                    }
                }
                string mergedCss = sb.ToString();
                Console.WriteLine($"[ThemeLoader][LoadCssFiles] Merged CSS content length: {mergedCss.Length} bytes");
                
                //傳給 JS 套用
                await JS.InvokeVoidAsync("applyDynamicCss", mergedCss);
                Console.WriteLine($"[ThemeLoader][LoadCssFiles] Applied merged CSS to the document.");
            }
        }
    }
}