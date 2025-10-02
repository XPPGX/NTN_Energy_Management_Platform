using Microsoft.AspNetCore.Mvc;
using System.IO;
using demoVer.Services;
using demoVer.Utils;
using demoVer.Models;

[Route("api/FileDownload")]
[ApiController]
public class FileDownloadController : ControllerBase
{
    private string _category = string.Empty;
    private readonly string _exportDirectory = "exports";
    private readonly DataCenter _dataCenter;
    
    public FileDownloadController(DataCenter dataCenter)
    {
        _dataCenter = dataCenter;

        _category = GetType().FullName!;
    }

    [HttpGet("{date}/{fileName}")]
    public IActionResult DownloadFile(string date, string fileName)
    {
        var dir = _dataCenter.get_basePath();
        dir = Path.Combine(dir, _exportDirectory);
        dir = Path.Combine(dir, date);
        var filePath = Path.Combine(dir, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        //刪掉檔案
        HttpContext.Response.OnCompleted(() =>
        {
            try
            {
                if(Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                    AppLogger.Log_To_File_log(_category, $"[FileDownloadController][DownloadFile] 已刪除檔案與資料夾", AppLogLevel.Trace);
                }
                // if (System.IO.File.Exists(filePath))
                // {
                //     System.IO.File.Delete(filePath);
                //     AppLogger.Log_To_File_log()
                //     Console.WriteLine($"[FileDownloadController][DownloadFile] 已刪除檔案: {filePath}");
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileDownloadController][DownloadFile] 刪除檔案失敗: {ex.Message}");
            }
            return Task.CompletedTask;
        });
        

        return PhysicalFile(filePath, "text/csv", fileName);
    }
}
