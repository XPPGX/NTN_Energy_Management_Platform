

using System.Diagnostics;
using System.Threading.Tasks;


namespace demoVer.Service_demo
{
    public class CanBusService
    {
        public async Task<bool> SendCanMessageAsync(string canInterface, string message)
        {
            try
            {
                // 例如：cansend can0 123#deadbeef
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/cansend",
                        Arguments = $"{canInterface} {message}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                // 你可以根據 output 或 error 來判斷是否成功
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
