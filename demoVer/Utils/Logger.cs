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
}
