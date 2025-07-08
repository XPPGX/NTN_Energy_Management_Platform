using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;

namespace demoVer.Utils
{
    public static class AppLogger
    {
        public static bool DEBUG_MODE = true;

        public static void Log(string message, bool local_debug)
        {
            if (DEBUG_MODE && local_debug)
            {
                Console.WriteLine($"[Debug] {message}");
            }
        }
    }


    public static class AppJsonManager
    {
        public static bool DEBUG_MODE = true;
        public static string Get_Writable_DataPath(string filename)
        {
            string basePath;
            // if(OperatingSystem.IsLinux())
            // {
            //     string home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
            //     basePath = Path.Combine(home, ".config", "App_Data");
            // }
            // else if(OperatingSystem.IsWindows())
            // {
            //     string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            //     basePath = Path.Combine(localAppData, "App_Data");
            // }
            // else
            // {
            //     basePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            // }

            basePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");

            Directory.CreateDirectory(basePath);
            return Path.Combine(basePath, filename);
        }

        public static void SavePartialJson(string filePath, string key, object data)
        {
            JsonObject root;
            
            if(File.Exists(filePath))
            {
                var text = File.ReadAllText(filePath);
                root = JsonSerializer.Deserialize<JsonObject>(text) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var node = JsonSerializer.SerializeToNode(data);
            root[key] = node;

            var options = new JsonSerializerOptions{WriteIndented = true};

            File.WriteAllText(filePath, JsonSerializer.Serialize(root, options));
        }
    }
}
