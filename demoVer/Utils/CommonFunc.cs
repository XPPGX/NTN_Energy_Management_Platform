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

    public static class ScalingComputer
    {
        public static float DevideOperation(float value, float factor)
        {
            switch(factor)
            {
                case 0.001f: return value * 1000;
                case 0.01f:  return value * 100;
                case 0.1f:   return value * 10;
                case 1.0f:   return value * 1;
                // case 10f:    return value / 10;
                // case 100f:   return value / 100;
                default:    return -1;
            }
        }

        public static float MultOperation(float value, float factor)
        {
            return AutoSnapToDecimal(value * factor, 3);
        }

        public static float AutoSnapToDecimal(float value, int decimals = 2)
        {
            float rounded = (float)Math.Round(value, decimals);
            float tolerance = MathF.Pow(10, -decimals) * 5;
            return Math.Abs(value - rounded) < tolerance ? rounded : value;
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
