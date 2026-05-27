using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NorthwoodLib.Logging;

namespace NorthwoodLib;

/// <summary>
/// Detects usage of <see href="https://www.winehq.org/">Wine</see> and informs about its version
/// </summary>
public static unsafe class WineInfo
{
	private const string Ntdll = "ntdll";

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
	public static readonly string? WineVersion;

	/// <summary>
	/// Returns used Wine Staging patches
	/// </summary>
	[Obsolete("Always returns null since Wine removed the API")]
	public static readonly string? WinePatches = null;

	/// <summary>
	/// Returns used Wine host
	/// </summary>
	public static readonly string? WineHost;

	static WineInfo()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			// Wine will always report as Windows
			return;

		string? wineVersion = GetWineVersion(out string? pinvokeError);

		if (!string.IsNullOrWhiteSpace(wineVersion))
		{
			WineVersion = $"Wine {wineVersion}";
			UsesWine = true;
		}

		if (ParseDll(ref UsesWine, ref UsesProton))
		{
			// Wine hidden in winecfg
			WineVersion = "Wine Hidden";
			PlatformSettings.Log("Detected hidden Wine", LogType.Debug);
		}
		else if (!UsesWine)
		{
			// not using Wine, ignore
			PlatformSettings.Log($"Wine not detected: {pinvokeError}", LogType.Debug);
			return;
		}

		string? wineBuild = GetWineBuild();
		if (!string.IsNullOrWhiteSpace(wineBuild))
			WineVersion = wineBuild;

		if (UsesProton)
			WineVersion = $"Proton based on {WineVersion}";

		WineHost = GetHostVersion();

		if (!string.IsNullOrWhiteSpace(WineHost))
			WineVersion += $" Host: {WineHost}";

		WineVersion = WineVersion!.Trim();

		PlatformSettings.Log($"{WineVersion} detected", LogType.Info);
	}

	private static string? GetWineVersion(out string? pinvokeError)
	{
		pinvokeError = null;
		try
		{
			// this will fail on Windows or when user disables exporting wine_get_version
			return Marshal.PtrToStringUTF8((nint) GetWineVersion());
		}
		catch (Exception exception)
		{
			pinvokeError = exception.Message;
		}

		return null;
	}

	private static string? GetWineBuild()
	{
		try
		{
			return Marshal.PtrToStringUTF8((nint) GetWineBuildId());
		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Wine build not detected: {ex.Message}", LogType.Debug);
		}

		return null;
	}

	private static string? GetHostVersion()
	{
		try
		{
			byte* sysnamePtr = null;
			byte* releasePtr = null;
			GetWineHostVersion(&sysnamePtr, &releasePtr);

			string? sysname = Marshal.PtrToStringUTF8((nint) sysnamePtr)?.TrimEnd();
			string? release = Marshal.PtrToStringUTF8((nint) releasePtr)?.TrimStart();
			if (!string.IsNullOrEmpty(sysname) || !string.IsNullOrEmpty(release))
				return $"{sysname} {release}".Trim();
		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Wine host not detected: {ex.Message}", LogType.Debug);
		}

		return null;
	}

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

	private static bool ParseDll(ref bool usesWine, ref bool usesProton)
	{
		bool hidden = false;
		try
		{
			using MemoryMappedFile kernelBase = MemoryMappedFile.CreateFromFile(Path.Combine(Environment.SystemDirectory, "kernelbase.dll"),
				FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
			using MemoryMappedViewAccessor kernelAccessor = kernelBase.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
			using SafeMemoryMappedViewHandle viewHandle = kernelAccessor.SafeMemoryMappedViewHandle;

			byte* ptr = null;
			viewHandle.AcquirePointer(ref ptr);
			try
			{
				ReadOnlySpan<byte> bytes = new(ptr, (int) Math.Min(viewHandle.ByteLength, 20 * 1024 * 1024));
				if (!usesWine)
				{
					usesWine = bytes.IndexOf("Wine "u8) >= 0;
					hidden = true;
				}

				if (!usesWine)
					return false;

				usesProton = bytes.IndexOf("Proton "u8) >= 0;
			}
			finally
			{
				viewHandle.ReleasePointer();
			}
		}
		catch (Exception exception)
		{
			PlatformSettings.Log($"Error checking OS files for Wine: {exception}", LogType.Error);
		}

		return hidden;
	}
}
