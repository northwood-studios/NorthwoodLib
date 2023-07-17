using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NorthwoodLib.Logging;

namespace NorthwoodLib
{
	/// <summary>
	/// Detects usage of <see href="https://www.winehq.org/">Wine</see> and informs about its version
	/// </summary>
	public static unsafe class WineInfo
	{
		private const string Ntdll = "ntdll";

		/// <summary>
		/// Returns used Wine version <see href="https://wiki.winehq.org/Developer_FAQ#How_can_I_detect_Wine.3F"/>
		/// </summary>
		/// <returns>Used Wine version</returns>
		[DllImport(Ntdll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_version")]
		private static extern byte* GetWineVersion();

		/// <summary>
		/// Returns used Wine build <see href="https://source.winehq.org/git/wine.git/blob/HEAD:/include/wine/library.h"/>
		/// </summary>
		/// <returns>Used Wine build</returns>
		[DllImport(Ntdll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_build_id")]
		private static extern byte* GetWineBuildId();

		/// <summary>
		/// Returns Wine host <see href="https://source.winehq.org/git/wine.git/blob/HEAD:/include/wine/library.h"/>
		/// </summary>
		/// <returns>Used Wine host</returns>
		[DllImport(Ntdll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_host_version")]
		private static extern void GetWineHostVersion(byte** sysname, byte** release);

		/// <summary>
		/// Informs if user uses Wine. Detection isn't fully reliable so don't rely on this for anything but diagnostics
		/// </summary>
		public static readonly bool UsesWine;

		/// <summary>
		/// Informs if user uses Proton. Detection isn't fully reliable so don't rely on this for anything but diagnostics
		/// </summary>
		public static readonly bool UsesProton;

		/// <summary>
		/// Returns used Wine Version
		/// </summary>
		public static readonly string WineVersion;

		/// <summary>
		/// Returns used Wine Staging patches
		/// </summary>
		[Obsolete("Always returns null since Wine removed the API")]
		public static readonly string WinePatches = null;

		/// <summary>
		/// Returns used Wine host
		/// </summary>
		public static readonly string WineHost;

		static WineInfo()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Wine will always report as Windows
				UsesWine = false;
				WineVersion = null;
				return;
			}

			ReadOnlySpan<byte> kernelBase = default;
			try
			{
				kernelBase = File.ReadAllBytes(Path.Combine(Environment.SystemDirectory, "kernelbase.dll"));

				// avoid parsing huge files
				if (kernelBase.Length > 20 * 1024 * 1024)
					kernelBase = default;
			}
			catch
			{
				// ignored
			}

			try
			{
				// this will fail on Windows or when user disables exporting wine_get_version
				string wineVersion = Marshal.PtrToStringAnsi((nint)GetWineVersion());

				if (string.IsNullOrWhiteSpace(wineVersion))
				{
					UsesWine = false;
					WineVersion = null;
					return;
				}

				UsesWine = true;
				WineVersion = $"Wine {wineVersion}";
			}
			catch (Exception ex)
			{
				if (kernelBase.IndexOf("Wine "u8) >= 0)
				{
					// not using Wine, ignore
					PlatformSettings.Log($"Detected hidden Wine", LogType.Debug);
					UsesWine = false;
				}
				else
				{
					// not using Wine, ignore
					PlatformSettings.Log($"Wine not detected: {ex.Message}", LogType.Debug);
					UsesWine = false;
				}
				WineVersion = null;
			}

			UsesProton = kernelBase.IndexOf("Proton "u8) >= 0;

			if (WineVersion == null)
				return;

			try
			{
				string wineBuild = Marshal.PtrToStringAnsi((nint)GetWineBuildId());

				if (!string.IsNullOrWhiteSpace(wineBuild))
					WineVersion += $" {wineBuild}";
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Wine build not detected: {ex.Message}", LogType.Debug);
			}

			try
			{
				byte* sysnamePtr = null;
				byte* releasePtr = null;
				GetWineHostVersion(&sysnamePtr, &releasePtr);
				string sysname = Marshal.PtrToStringAnsi((nint)sysnamePtr)?.Trim() ?? "";
				string release = Marshal.PtrToStringAnsi((nint)releasePtr)?.Trim() ?? "";
				if (sysname != "" || release != "")
				{
					WineHost = "Host:";
					if (!string.IsNullOrWhiteSpace(sysname))
						WineHost += $" {sysname}";
					if (!string.IsNullOrWhiteSpace(release))
						WineHost += $" {release}";
					WineVersion += $" Host:{WineHost}";
				}
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Wine host not detected: {ex.Message}", LogType.Debug);
			}

			WineVersion = WineVersion.Trim();

			PlatformSettings.Log($"{WineVersion} detected", LogType.Info);
		}
	}
}
