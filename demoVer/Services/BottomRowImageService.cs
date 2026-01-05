using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Forms;

namespace demoVer.Services;

public class BottomRowImageService
{
    // 使用集中配置，方便移植
    public const string AppPort = BottomRowConstants.AppPort;
    public const int MaxButtonCount = BottomRowConstants.MaxButtonCount;
    private const long MaxUploadBytes = BottomRowConstants.MaxUploadBytes;

    // We store the file as BottomImg{n}.{ext} (no conversion).

    public string GetDefaultImageUrl(int index)
    {
        ValidateIndex(index);
        return BottomRowConstants.DefaultImagePaths[index - 1];
    }

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg"
    };

    /// <summary>
    /// Get the base directory for UserSetting (without AppPort subfolder).
    /// Used for configuring static file provider in Program.cs.
    /// </summary>
    public static string GetUserSettingBaseDir()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? Path.Combine("/userdata/CMU3/UserSetting", AppPort)
            : Path.Combine(Directory.GetCurrentDirectory(), "UserSetting");
    }

    public bool TryGetExistingImage(int index, out string filePath, out string url)
    {
        filePath = string.Empty;
        url = string.Empty;

        try
        {
            ValidateIndex(index);
            var dir = GetAppPortDir();
            var prefix = $"BottomImg{index}";

            if (!Directory.Exists(dir))
            {
                return false;
            }

            var candidates = Directory.EnumerateFiles(dir, prefix + ".*")
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var candidate in candidates)
            {
                var ext = Path.GetExtension(candidate);
                if (!AllowedExtensions.Contains(ext))
                {
                    continue;
                }

                filePath = candidate;
                url = $"/UserSetting/{AppPort}/{Path.GetFileName(candidate)}";
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool ImageExists(int index) => TryGetExistingImage(index, out _, out _);

    public string GetImageUrl(int index)
    {
        return TryGetExistingImage(index, out _, out var url) ? url : string.Empty;
    }

    public long? GetImageLastWriteTicksUtc(int index)
    {
        try
        {
            if (!TryGetExistingImage(index, out var path, out _)) return null;
            return File.GetLastWriteTimeUtc(path).Ticks;
        }
        catch
        {
            return null;
        }
    }

    // === Temporary image methods (for preview before save) ===

    public bool TryGetTempImage(int index, out string filePath, out string url)
    {
        filePath = string.Empty;
        url = string.Empty;

        try
        {
            ValidateIndex(index);
            var dir = GetAppPortDir();
            var prefix = $"BottomImg{index}_temp";

            if (!Directory.Exists(dir))
            {
                return false;
            }

            var candidates = Directory.EnumerateFiles(dir, prefix + ".*")
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var candidate in candidates)
            {
                var ext = Path.GetExtension(candidate);
                if (!AllowedExtensions.Contains(ext))
                {
                    continue;
                }

                filePath = candidate;
                url = $"/UserSetting/{AppPort}/{Path.GetFileName(candidate)}";
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool TempImageExists(int index) => TryGetTempImage(index, out _, out _);

    public string GetTempImageUrl(int index)
    {
        return TryGetTempImage(index, out _, out var url) ? url : string.Empty;
    }

    public long? GetTempImageLastWriteTicksUtc(int index)
    {
        try
        {
            if (!TryGetTempImage(index, out var path, out _)) return null;
            return File.GetLastWriteTimeUtc(path).Ticks;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> SaveTempAsync(int index, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        ValidateIndex(index);
        if (file is null) throw new ArgumentNullException(nameof(file));

        // Validate as image
        var ext = NormalizeExtension(file.Name);
        if (!IsImageFile(file.ContentType, ext))
        {
            throw new InvalidOperationException("上傳檔案必須是圖檔。");
        }

        var dir = GetAppPortDir();
        Directory.CreateDirectory(dir);

        // Remove older temp versions with different extensions
        DeleteTempVariants(index);

        var fileName = $"BottomImg{index}_temp{ext}";
        var filePath = Path.Combine(dir, fileName);

        try
        {
            using (var read = file.OpenReadStream(MaxUploadBytes, cancellationToken))
            {
                // Use FileStream with FileShare.Read to allow other processes to read the file
                using (var write = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await read.CopyToAsync(write, cancellationToken);
                    await write.FlushAsync(cancellationToken);
                }
            }
            
            // Small delay to ensure file handle is fully released
            await Task.Delay(50, cancellationToken);
        }
        catch (Exception ex)
        {
            // Clean up partial files
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
            throw new IOException($"存圖失敗：{ex.Message}", ex);
        }

        return $"/UserSetting/{AppPort}/{fileName}";
    }

    public void CommitTempImage(int index)
    {
        try
        {
            ValidateIndex(index);
            if (!TryGetTempImage(index, out var tempPath, out _))
            {
                return; // No temp image to commit
            }

            var ext = Path.GetExtension(tempPath);
            var dir = GetAppPortDir();
            var targetFileName = $"BottomImg{index}{ext}";
            var targetPath = Path.Combine(dir, targetFileName);

            // Delete old official image
            DeleteAllVariants(index);

            // Move temp to official
            File.Move(tempPath, targetPath, overwrite: true);
        }
        catch
        {
            // Ignore errors
        }
    }

    public void DeleteTempVariants(int index)
    {
        try
        {
            ValidateIndex(index);
            var dir = GetAppPortDir();
            var prefix = $"BottomImg{index}_temp";
            if (!Directory.Exists(dir)) return;

            foreach (var file in Directory.EnumerateFiles(dir, prefix + ".*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    public async Task<string> SaveAsync(int index, IBrowserFile file, CancellationToken cancellationToken = default)
    {
        ValidateIndex(index);
        if (file is null) throw new ArgumentNullException(nameof(file));

        // Validate as image
        var ext = NormalizeExtension(file.Name);
        if (!IsImageFile(file.ContentType, ext))
        {
            throw new InvalidOperationException("上傳檔案必須是圖檔。");
        }

        var dir = GetAppPortDir();
        Directory.CreateDirectory(dir);

        // Remove older versions with different extensions
        DeleteAllVariants(index);

        var fileName = $"BottomImg{index}{ext}";
        var filePath = Path.Combine(dir, fileName);

        try
        {
            using (var read = file.OpenReadStream(MaxUploadBytes, cancellationToken))
            {
                // Use FileStream with FileShare.Read to allow other processes to read the file
                using (var write = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await read.CopyToAsync(write, cancellationToken);
                    await write.FlushAsync(cancellationToken);
                }
            }
            
            // Small delay to ensure file handle is fully released
            await Task.Delay(50, cancellationToken);
        }
        catch (Exception ex)
        {
            // Clean up partial files
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
            throw new IOException($"存圖失敗：{ex.Message}", ex);
        }

        return $"/UserSetting/{AppPort}/{fileName}";
    }

    public void DeleteAllVariants(int index)
    {
        try
        {
            ValidateIndex(index);
            var dir = GetAppPortDir();
            var prefix = $"BottomImg{index}";
            if (!Directory.Exists(dir)) return;

            foreach (var file in Directory.EnumerateFiles(dir, prefix + ".*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private string GetAppPortDir()
    {
        var baseDir = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "/userdata/CMU3/UserSetting"
            : Path.Combine(Directory.GetCurrentDirectory(), "UserSetting");

        return Path.Combine(baseDir, AppPort);
    }

    private static string NormalizeExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
        {
            return ".png"; // fallback; still requires content-type to be image/*
        }

        ext = ext.Trim();
        if (!ext.StartsWith('.')) ext = "." + ext;
        return ext.ToLowerInvariant();
    }

    private static bool IsImageFile(string? contentType, string ext)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            // If browser says it's an image, accept.
            return true;
        }

        // Fallback to extension allowlist
        return AllowedExtensions.Contains(ext);
    }

    private static void ValidateIndex(int index)
    {
        if (index < 1 || index > MaxButtonCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"index must be 1..{MaxButtonCount}");
        }
    }
}
