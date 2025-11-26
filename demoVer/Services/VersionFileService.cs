// VersionFileService.cs
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using demoVer.Utils;
using demoVer.Models;
using System.Runtime.InteropServices;
namespace demoVer.Services
{
	/// <summary>
	/// 啟動時負責輸出版本檔的服務。
	/// 規格：
	/// - 路徑：/userdata/CMU3/Application/
	/// - 檔名：<AppImageBaseName>_version.json(若未知則使用 unknown_version.json)
	/// - 內容：五個核心欄位 + 建議衍生欄位 + appImageName + appImageSize
	/// - 覆蓋條件：僅當五個核心欄位任一不同時才覆蓋；完全相同則跳過寫入
	/// </summary>
	public sealed class VersionFileService
	{
        private string _category = "";
		private const string OutputDir = "/userdata/CMU3/Application";

		public VersionFileService()
		{
			_category = GetType().FullName!;
		}

		/// <summary>
		/// 產出或更新版本檔。如果舊檔的五個核心欄位與目前版本一致，則跳過寫入。
		/// 任何例外都只記錄 log，不會往外拋出以免影響啟動流程。
		/// </summary>
		public async Task WriteVersionFileAsync(CancellationToken ct = default)
		{
			try
			{
				// 1) 收集資料(核心五欄 + 衍生 + AppImage 資訊)
				var (appImageName, appImageSize) = ResolveAppImageInfo();
				var payload = BuildPayload(appImageName, appImageSize);

				// 2) 目錄與檔名
				Directory.CreateDirectory(OutputDir);
				var versionFileName = GetVersionFileName(appImageName);
				var finalPath = Path.Combine(OutputDir, versionFileName);

				// 3) 若舊檔存在，先比較五個核心欄位；全部相同則跳過
				if (File.Exists(finalPath))
				{
					try
					{
						using var fs = File.OpenRead(finalPath);
						using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
						if (CoreFieldsEqual(doc, payload))
						{
							AppLogger.Log_To_File_log(_category, $"Version file unchanged (five core fields are equal). Skip writing: {finalPath}", AppLogLevel.Debug);
							return;
						}
					}
					catch (Exception ex)
					{
						// 舊檔壞掉或解析失敗，視同需要重寫
						AppLogger.Log_To_File_log(_category, $"Existing version file parse failed. Will rewrite: {finalPath} Exception: {ex}", AppLogLevel.Debug);
					}
				}

				// 4) 原子寫入(.tmp → Move 覆蓋)
				var json = JsonSerializer.Serialize(payload, JsonOptions);
				var tmpPath = finalPath + ".tmp";

				await File.WriteAllTextAsync(tmpPath, json, ct);
#if NET8_0_OR_GREATER
				File.Move(tmpPath, finalPath, overwrite: true);
#else
				if (File.Exists(finalPath)) File.Delete(finalPath);
				File.Move(tmpPath, finalPath);
#endif
				AppLogger.Log_To_File_log(_category, $"Version file written: {finalPath} (ext={payload.extVersionHex}, compat={payload.compatVersionHex}, branch={payload.branchLetter}, date={payload.releaseDate}, seq={payload.intraDaySeq}, size={payload.appImageSize})", AppLogLevel.Debug);
			}
			catch (OperationCanceledException)
			{
				AppLogger.Log_To_File_log(_category, "WriteVersionFileAsync was canceled.", AppLogLevel.Debug);
			}
			catch (Exception ex)
			{
				AppLogger.Log_To_File_log(_category, $"Failed to write version file. Exception: {ex}", AppLogLevel.Debug);
			}
		}

		// ---------- Internal: Build Payload ----------

		private static VersionFilePayload BuildPayload(string appImageName, long appImageSize)
		{
			var v = AppVersion.Current;

			// 依規格輸出五個核心欄位 + 衍生欄位
			var release = v.ReleaseDateUtc.Date; // yyyy-MM-dd(避免時區干擾)
			return new VersionFilePayload
			{
				// 核心 5 欄
				extVersionHex = To0xHex(v.Ext),
				compatVersionHex = To0xHex(v.Compat),
				branchLetter = v.BranchLetter.ToString(),
				releaseDate = release.ToString("yyyy-MM-dd"),
				intraDaySeq = v.IntraDaySeq,

				// 衍生欄位
				externalVersionString = v.ExternalVersionString,	// e.g., "0.1"
				shortDisplay = AppVersion.Short,					// e.g., "0.1-G-20251021-1"
				hexBytes = AppVersion.Hex,							// "01-01-47-19-0A-89"

				// AppImage 資訊
				appImageName = appImageName,
				appImageSize = appImageSize
			};
		}

		// ---------- Internal: Core Field Comparison ----------

		/// <summary>
		/// 比較舊檔 JSON 與新 payload 的五個核心欄位是否一致。
		/// 任一不同即回傳 false。
		/// </summary>
		private static bool CoreFieldsEqual(JsonDocument oldDoc, VersionFilePayload newPayload)
		{
			try
			{
				var root = oldDoc.RootElement;

				string? extOld = TryGetString(root, "extVersionHex");
				string? compatOld = TryGetString(root, "compatVersionHex");
				string? branchOld = TryGetString(root, "branchLetter");
				string? dateOld = TryGetString(root, "releaseDate");
				int? seqOld = TryGetInt(root, "intraDaySeq");

				if (extOld is null || compatOld is null || branchOld is null || dateOld is null || seqOld is null)
					return false; // 任一缺失 → 視為不同，需重寫

				return
					StringEqualsOrdinal(extOld, newPayload.extVersionHex) &&
					StringEqualsOrdinal(compatOld, newPayload.compatVersionHex) &&
					StringEqualsOrdinal(branchOld, newPayload.branchLetter) &&
					StringEqualsOrdinal(dateOld, newPayload.releaseDate) &&
					seqOld.Value == newPayload.intraDaySeq;
			}
			catch
			{
				return false;
			}
		}

		private static string? TryGetString(JsonElement root, string name)
			=> root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;

		private static int? TryGetInt(JsonElement root, string name)
			=> root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var v) ? v : null;

		private static bool StringEqualsOrdinal(string a, string b)
			=> string.Equals(a, b, StringComparison.Ordinal);

		private static string To0xHex(byte b)
			=> $"0x{b:X2}";

		// ---------- Internal: Resolve AppImage Info ----------

		private (string appImageName, long appImageSize) ResolveAppImageInfo()
		{
			// 1) 優先使用 APPIMAGE 環境變數(AppImage runtime 設定)
			var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
			if (!string.IsNullOrWhiteSpace(appImagePath))
			{
				var name = Path.GetFileName(appImagePath);
				var size = SafeGetFileSize(appImagePath);
				return (name, size);
			}

			// 2) 嘗試以目前程序檔名推回預設目錄(/userdata/CMU3/Application)
			try
			{
				var procName = AppDomain.CurrentDomain.FriendlyName; // 可能是 "CMU3" 或執行檔名
				if (!string.IsNullOrWhiteSpace(procName))
				{
					var guessName = EnsureAppImageName(procName); // 確保有 .AppImage 副檔名
					var candidate = Path.Combine(OutputDir, guessName);
					if (File.Exists(candidate))
						return (guessName, SafeGetFileSize(candidate));
				}
			}
			catch
			{
				// ignore
			}

			// 3) 都抓不到就標記 unknown；size=0
			return ("unknown", 0);
		}

		private static string EnsureAppImageName(string nameOrPath)
		{
			var baseName = Path.GetFileName(nameOrPath);
			if (string.IsNullOrEmpty(Path.GetExtension(baseName)))
				return baseName + ".AppImage";
			return baseName;
		}

		private static long SafeGetFileSize(string path)
		{
			try
			{
				var fi = new FileInfo(path);
				return fi.Exists ? fi.Length : 0;
			}
			catch
			{
				return 0;
			}
		}

		private static string GetVersionFileName(string appImageName)
		{
			if (string.IsNullOrWhiteSpace(appImageName) || string.Equals(appImageName, "unknown", StringComparison.OrdinalIgnoreCase))
				return "unknown_version.json";

			var baseName = Path.GetFileNameWithoutExtension(appImageName);
			if (string.IsNullOrEmpty(baseName))
				baseName = "unknown";
			return $"{baseName}_version.json";
		}

		// ---------- JSON Options ----------

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = global::System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 檔名若有非 ASCII 也能輸出
		};

		// ---------- DTO (exact field names in camelCase) ----------

		private sealed class VersionFilePayload
		{
			// Core 5 fields
			[JsonPropertyName("extVersionHex")] public string extVersionHex { get; set; } = "";
			[JsonPropertyName("compatVersionHex")] public string compatVersionHex { get; set; } = "";
			[JsonPropertyName("branchLetter")] public string branchLetter { get; set; } = "";
			[JsonPropertyName("releaseDate")] public string releaseDate { get; set; } = ""; // "yyyy-MM-dd"
			[JsonPropertyName("intraDaySeq")] public int intraDaySeq { get; set; }

			// Derived convenience fields
			[JsonPropertyName("externalVersionString")] public string externalVersionString { get; set; } = "";
			[JsonPropertyName("shortDisplay")] public string shortDisplay { get; set; } = "";
			[JsonPropertyName("hexBytes")] public string hexBytes { get; set; } = "";

			// AppImage info
			[JsonPropertyName("appImageName")] public string appImageName { get; set; } = "";
			[JsonPropertyName("appImageSize")] public long appImageSize { get; set; }
		}
	}
}
