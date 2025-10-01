using Microsoft.AspNetCore.Mvc;
using System.IO;


[Route("api/[controller]")]
[ApiController]
public class FileDownloadController : ControllerBase
{
    private readonly string _exportDirectory = "/userdata/CMU3/UserSetting/5040/exports";

    [HttpGet("{date}/{fileName}")]
    public IActionResult DownloadFile(string date, string fileName)
    {
        var dir = Path.Combine(_exportDirectory, date);
        var filePath = Path.Combine(dir, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        return PhysicalFile(filePath, "text/csv", fileName);
    }
}
