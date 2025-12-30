using System;
using System.Globalization;

namespace demoVer.Utils
{
	public static class AppVersion
	{
		// ================= 設定參數區=================
		private const byte ExtVersion					= 0x01;							// 對外版：0x01=0.1、0x0A=1.0
		private const byte CompatVersion				= 0x01;							// 相容版：通常 = ExtVersion
		private const char BranchLetter					= 'G';							// 分支字母
		private static readonly DateTime ReleaseDate	= new DateTime(2025, 12, 30);	// 發佈日期(UTC/本地都可，僅取年月日)
		private const byte IntraDaySeq					= 0;							// 當天序號 0..7
		// ===============================================

		public static readonly VersionId Current = VersionId.Build(
			ExtVersion, CompatVersion, BranchLetter, ReleaseDate, IntraDaySeq);

		public static byte[] Bytes => Current.ToBytes();
		public static string Hex => Current.ToHexDash();
		public static string Short => Current.ToShortDisplay();
	}

	public readonly struct VersionId
	{
		public readonly byte Ext;		// [0]
		public readonly byte Compat;	// [1]
		public readonly byte Branch;	// [2]
		public readonly byte YearOff;	// [3]
		public readonly byte Month;		// [4]
		public readonly byte DayAndSeq;	// [5]

		public VersionId(byte ext, byte compat, byte branchAscii, byte yearOff, byte month, byte dayAndSeq)
		{
			Ext = ext; Compat = compat; Branch = branchAscii; YearOff = yearOff; Month = month; DayAndSeq = dayAndSeq;
		}

		public byte[] ToBytes() => new[] { Ext, Compat, Branch, YearOff, Month, DayAndSeq };

		public static VersionId FromBytes(ReadOnlySpan<byte> six)
		{
			if (six.Length != 6) throw new ArgumentException("VersionId needs exactly 6 bytes.");
			return new VersionId(six[0], six[1], six[2], six[3], six[4], six[5]);
		}

		public static VersionId Build(byte extVersion, byte compatVersion, char branchLetter, DateTime releaseDate, byte intraDaySeq)
		{
			if (releaseDate.Year < 2000 || releaseDate.Year > 2255) throw new ArgumentOutOfRangeException(nameof(releaseDate));
			if (intraDaySeq > 7) throw new ArgumentOutOfRangeException(nameof(intraDaySeq));
			byte yearOff = (byte)(releaseDate.Year - 2000);
			byte month = (byte)releaseDate.Month;
			byte day = (byte)releaseDate.Day;
			byte dayAndSeq = (byte)((day << 3) | (intraDaySeq & 0x07)); // <<3 才裝得下
			return new VersionId(extVersion, compatVersion, (byte)branchLetter, yearOff, month, dayAndSeq);
		}

		public int Day => DayAndSeq >> 3;
		public int IntraDaySeq => DayAndSeq & 0x07;
		public DateTime ReleaseDateUtc => new DateTime(2000 + YearOff, Month, Day, 0, 0, 0, DateTimeKind.Utc);
		public string ExternalVersionString => $"{Ext / 10}.{Ext % 10}";
		public char BranchLetter => (char)Branch;
		public string ToShortDisplay()
		{
			var d = ReleaseDateUtc;
			return $"{ExternalVersionString}-{BranchLetter}-{d:yyyyMMdd}-{IntraDaySeq}";
		}
		public string ToHexDash() => BitConverter.ToString(ToBytes());
		public static VersionId FromHexDash(string hex)
		{
			var clean = hex.Replace("-", "", StringComparison.OrdinalIgnoreCase).Trim();
			if (clean.Length != 12) throw new FormatException("Hex must be 12 hex chars for 6 bytes.");
			Span<byte> buf = stackalloc byte[6];
			for (int i = 0; i < 6; i++)
				buf[i] = byte.Parse(clean.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			return FromBytes(buf);
		}
	}


	
}
