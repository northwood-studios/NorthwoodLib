using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using NorthwoodLib.Logging;

namespace NorthwoodLib;

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

		static MemoryMappedFile GetKernelBaseMemoryMap()
		{
			try
			{
				return MemoryMappedFile.CreateFromFile(Path.Combine(Environment.SystemDirectory, "kernelbase.dll"),
					FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
			}
			catch
			{
				return null;
			}
		}

		using MemoryMappedFile kernelBase = GetKernelBaseMemoryMap();
		using MemoryMappedViewAccessor kernelAccessor = kernelBase?.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
		byte* bytes = null;
		kernelAccessor?.SafeMemoryMappedViewHandle.AcquirePointer(ref bytes);
		ReadOnlySpan<byte> kernelBytes = kernelBase == null ? default :
			new ReadOnlySpan<byte>(bytes, (int) Math.Min(kernelAccessor.SafeMemoryMappedViewHandle.ByteLength, 20 * 1024 * 1024));

		try
		{
			// this will fail on Windows or when user disables exporting wine_get_version
			string wineVersion = Marshal.PtrToStringAnsi((nint) GetWineVersion());

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
			if (kernelBytes.IndexOf("Wine "u8) < 0)
			{
				// not using Wine, ignore
				PlatformSettings.Log($"Wine not detected: {ex.Message}", LogType.Debug);
				UsesWine = false;
				WineVersion = null;
				return;
			}

			// Wine hidden in winecfg
			PlatformSettings.Log("Detected hidden Wine", LogType.Debug);
			UsesWine = true;
			WineVersion = "Wine Hidden";
		}

		try
		{
			string wineBuild = Marshal.PtrToStringAnsi((nint) GetWineBuildId());

			if (!string.IsNullOrWhiteSpace(wineBuild))
				WineVersion = wineBuild;
		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Wine build not detected: {ex.Message}", LogType.Debug);
		}

		UsesProton = kernelBytes.IndexOf("Proton "u8) >= 0;
		if (UsesProton)
			WineVersion = $"Proton based on {WineVersion}";

		kernelAccessor?.SafeMemoryMappedViewHandle.ReleasePointer();

		try
		{
			byte* sysnamePtr = null;
			byte* releasePtr = null;
			GetWineHostVersion(&sysnamePtr, &releasePtr);
			string sysname = Marshal.PtrToStringAnsi((nint) sysnamePtr)?.Trim() ?? "";
			string release = Marshal.PtrToStringAnsi((nint) releasePtr)?.Trim() ?? "";
			if (sysname != "" || release != "")
			{
				WineHost = sysname;
				if (!string.IsNullOrWhiteSpace(release))
					WineHost += $" {release}";
				WineHost = WineHost.Trim();
				WineVersion += $" Host: {WineHost}";
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
