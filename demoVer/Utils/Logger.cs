namespace demoVer.Utils
{
    public static class AppLogger
    {
        public static bool DEBUG_MODE = true;

        public static void Log(string message)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[Debug] {message}");
            }
        }

        public static void Warn(string message)
        {
            if (DEBUG_MODE)
            {
                Console.WriteLine($"[Warning] {message}");
            }
        }

        public static void Error(string message)
        {
            Console.WriteLine($"[Error] {message}");
        }
    }
}
