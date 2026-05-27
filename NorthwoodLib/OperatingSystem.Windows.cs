using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using NorthwoodLib.Logging;
using NorthwoodLib.Pools;

namespace NorthwoodLib;

public static unsafe partial class OperatingSystem
{
	private const nint Hklm = -2147483646;
	private const string RegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

	private static void GetWindowsVersion(out Version version, out string name)
	{
		if (TryGetVersion(false, out Version? winVersion, out bool server, out string? servicePack, out Version? servicePackVersion))
			version = winVersion;
		else if (TryGetVersion(true, out winVersion, out server, out servicePack, out servicePackVersion))
		{
			PlatformSettings.Log("Correcting system version using GetVersionExW", LogType.Warning);
			version = winVersion;
		}
		else if (TryGetWindowsRegistryVersion(out Version? fileVersion))
		{
			PlatformSettings.Log("Correcting system version using registry data", LogType.Warning);
			version = fileVersion;
		}
		else
			version = Environment.OSVersion.Version;

		servicePack ??= Environment.OSVersion.ServicePack;
		servicePackVersion ??= !string.IsNullOrEmpty(servicePack) &&
							   int.TryParse(servicePack.AsSpan(servicePack.Length - 1), out int spMajor) ? new Version(spMajor, 0) : null;

		if (version.Revision <= 0 && TryGetHklmDword(RegistryPath, "UBR", out uint ubr) && ubr > 0)
			version = new Version(version.Major, version.Minor, version.Build, (int) ubr);

		name = CreateDescription(version, server, servicePackVersion, servicePack);
	}

	private static bool TryGetVersion(bool useLegacy, [NotNullWhen(true)] out Version? version, out bool server, [NotNullWhen(true)] out string? servicePack,
		[NotNullWhen(true)] out Version? servicePackVersion)
	{
		OsVersionInfo osVersionInfo = new()
		{
			dwOSVersionInfoSize = (uint) sizeof(OsVersionInfo)
		};

		try
		{
			if (useLegacy)
			{
				int status = GetVersionFallback(&osVersionInfo);
				if (status != 0)
					throw new Win32Exception();
			}
			else
			{
				uint status = GetVersion(&osVersionInfo);
				if (status != 0)
					throw new Win32Exception(NtStatusToDosCode(status));
			}
			ParseWindowsVersion(osVersionInfo, out version, out server, out servicePack, out servicePackVersion);

			if (IsValidWindowsVersion(version))
				return true;
		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Failed to get Windows version! {ex.Message}", LogType.Warning);
		}

		version = null;
		server = false;
		servicePack = null;
		servicePackVersion = null;
		return false;
	}

	private static string CreateDescription(Version version, bool server, Version? servicePackVersion, string? servicePack)
	{
		StringBuilder nameBuilder = StringBuilderPool.Shared.Rent(WineInfo.UsesWine ? $"{WineInfo.WineVersion} " : "");
		nameBuilder.Append($"Windows {ProcessWindowsVersion(version, server, GetHklmString(RegistryPath, "DisplayVersion"))}");

		string? product = GetProductInfo(version, servicePackVersion);

		if (!string.IsNullOrWhiteSpace(product))
			nameBuilder.Append($" {product}");

		if (!string.IsNullOrWhiteSpace(servicePack))
			nameBuilder.Append($" {servicePack}");

		return StringBuilderPool.Shared.ToStringReturn(nameBuilder).Trim();
	}

	private static bool IsValidWindowsVersion(Version version)
	{
		return version != null && IsValidWindowsVersion(version.Major, version.Minor);
	}

	private static bool IsValidWindowsVersion(int major, int minor)
	{
		// 6.2 is Windows 8, Windows 8.1 and 10 sometimes like to identify themselves as it
		return major > 0 && minor >= 0 && (major != 6 || minor != 2);
	}

	private static void ParseWindowsVersion(OsVersionInfo osVersionInfo, out Version version, out bool server, out string servicePack, out Version servicePackVersion)
	{
		version = new Version((int) osVersionInfo.dwMajorVersion, (int) osVersionInfo.dwMinorVersion, (int) osVersionInfo.dwBuildNumber);
		// from https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
		server = osVersionInfo.wProductType != 1;
		servicePack = new string((char*) osVersionInfo.szCSDVersion);
		servicePackVersion = new Version(osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor);
	}

	/// <summary>
	/// Fetches the Windows version from registry
	/// </summary>
	/// <param name="version">System version</param>
	/// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
	internal static bool TryGetWindowsRegistryVersion([NotNullWhen(true)] out Version? version)
	{
		version = null;

		if (TryGetHklmDword(RegistryPath, "CurrentMajorVersionNumber", out uint major) || major <= 0)
			return false;

		if (TryGetHklmDword(RegistryPath, "CurrentMinorVersionNumber", out uint minor))
			return false;

		if ((!int.TryParse(GetHklmString(RegistryPath, "CurrentBuildNumber"), out int build) || build <= 0) &&
			(!int.TryParse(GetHklmString(RegistryPath, "CurrentBuild"), out build) || build <= 0))
			return false;

		version = new Version((int) major, (int) minor, build);
		return true;
	}

	private static string ProcessWindowsVersion(Version version, bool server, string? displayVersion)
	{
		if (string.IsNullOrWhiteSpace(displayVersion))
			displayVersion = null;

		string? versionText = null;
		switch (version.Major)
		{
			case 10 when version.Minor == 0 && server:
				{
					string main = version.Build switch
					{
						25398 => "Annual Channel",
						> 26040 => "2025",
						> 20000 => "2022",
						> 17677 => "2019",
						_ => "2016"
					};
					string patch = displayVersion ?? version.Build switch
					{
						26100 => "24H2",
						26040 => "23H2",
						25398 => "23H2",
						20348 => "21H2",
						19042 => "20H2",
						19041 => "2004",
						18363 => "1909",
						18362 => "1903",
						17763 => "1809",
						17134 => "1803",
						16299 => "1709",
						14393 => "1607",
						_ => $"build {version.Build}"
					};
					versionText = $"Server {main} {patch}";
					break;
				}
			case 10 when version.Minor == 0:
				{
					string main = version.Build switch
					{
						> 22000 => "11",
						_ => "10"
					};
					string patch = displayVersion ?? version.Build switch
					{
						26300 => "26H2",
						28000 => "26H1",
						26200 => "25H2",
						26100 => "24H2",
						22631 => "23H2",
						22621 => "22H2",
						22000 => "21H2",
						19045 => "22H2",
						19044 => "21H2",
						19043 => "21H1",
						19042 => "20H2",
						19041 => "2004",
						18363 => "1909",
						18362 => "1903",
						17763 => "1809",
						17134 => "1803",
						16299 => "1709",
						15063 => "1703",
						14393 => "1607",
						10586 => "1511",
						10240 => "1507",
						_ => $"build {version.Build}"
					};
					versionText = $"{main} {patch}";
					break;
				}
			case 6 when server:
				{
					versionText = version.Minor switch
					{
						3 => "Server 2012 R2",
						2 => "Server 2012",
						1 => "Server 2008 R2",
						0 => "Server 2008",
						_ => null
					};
					break;
				}
			case 6:
				{
					versionText = version.Minor switch
					{
						3 => "8.1",
						2 => "8",
						1 => "7",
						0 => "Vista",
						_ => null
					};
					break;
				}
		}

		return versionText ?? version.PrintVersion();
	}

	private static string? GetProductInfo(Version osVersion, Version? spVersion)
	{
		uint pdwReturnedProductType = 0;

		try
		{
			if (GetProductInfo((uint) osVersion.Major, (uint) osVersion.Minor,
					(uint) (spVersion?.Major ?? 0), (uint) (spVersion?.Minor ?? 0), &pdwReturnedProductType) == 0)
				return null;
		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Error while fetching the product info: {ex.Message}", LogType.Error);
			return null;
		}

		// from https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo
		return pdwReturnedProductType switch
		{
			0x00000006 => "Business",
			0x00000010 => "Business N",
			0x00000012 => "HPC Edition",
			0x00000040 => "Server Hyper Core V",
			0x00000065 => "Home",
			0x00000063 => "Home China",
			0x00000062 => "Home N",
			0x00000064 => "Home Single Language",
			0x00000050 => "Server Datacenter (evaluation installation)",
			0x00000091 => "Server Datacenter, Semi-Annual Channel (core installation)",
			0x00000092 => "Server Standard, Semi-Annual Channel (core installation)",
			0x00000008 => "Server Datacenter (full installation)",
			0x0000000C => "Server Datacenter (core installation)",
			0x00000027 => "Server Datacenter without Hyper-V (core installation)",
			0x00000025 => "Server Datacenter without Hyper-V (full installation)",
			0x00000079 => "Education",
			0x0000007A => "Education N",
			0x00000004 => "Enterprise",
			0x00000046 => "Enterprise E",
			0x00000048 => "Enterprise Evaluation",
			0x0000001B => "Enterprise N",
			0x00000054 => "Enterprise N Evaluation",
			0x0000007D => "Enterprise 2015 LTSB",
			0x00000081 => "Enterprise 2015 LTSB Evaluation",
			0x0000007E => "Enterprise 2015 LTSB N",
			0x00000082 => "Enterprise 2015 LTSB N Evaluation",
			0x0000000A => "Server Enterprise (full installation)",
			0x0000000E => "Server Enterprise (core installation)",
			0x00000029 => "Server Enterprise without Hyper-V (core installation)",
			0x0000000F => "Server Enterprise for Itanium-based Systems",
			0x00000026 => "Server Enterprise without Hyper-V (full installation)",
			0x0000003C => "Essential Server Solution Additional",
			0x0000003E => "Essential Server Solution Additional SVC",
			0x0000003B => "Essential Server Solution Management",
			0x0000003D => "Essential Server Solution Management SVC",
			0x00000002 => "Home Basic",
			0x00000043 => "Not supported",
			0x00000005 => "Home Basic N",
			0x00000003 => "Home Premium",
			0x00000044 => "Not supported",
			0x0000001A => "Home Premium N",
			0x00000022 => "Home Server 2011",
			0x00000013 => "Storage Server 2008 R2 Essentials",
			0x0000002A => "Microsoft Hyper-V Server",
			0x000000BC => "IoT Enterprise",
			0x000000BF => "IoT Enterprise LTSC",
			0x0000007B => "IoT Core",
			0x00000083 => "IoT Core Commercial",
			0x0000001E => "Essential Business Server Management Server",
			0x00000020 => "Essential Business Server Messaging Server",
			0x0000001F => "Essential Business Server Security Server",
			0x00000068 => "Mobile",
			0x00000085 => "Mobile Enterprise",
			0x0000004D => "MultiPoint Server Premium (full installation)",
			0x0000004C => "MultiPoint Server Standard (full installation)",
			0x00000077 => "Team",
			0x000000A4 => "Pro Education",
			0x000000A1 => "Pro for Workstations",
			0x000000A2 => "Pro for Workstations N",
			0x00000030 => "Pro",
			0x00000045 => "Not supported",
			0x00000031 => "Pro N",
			0x00000067 => "Professional with Media Center",
			0x00000032 => "Small Business Server 2011 Essentials",
			0x00000036 => "Server For SB Solutions EM",
			0x00000033 => "Server For SB Solutions",
			0x00000037 => "Server For SB Solutions EM",
			0x00000018 => "Server 2008 for Essential Server Solutions",
			0x00000023 => "Server 2008 without Hyper-V for Essential Server Solutions",
			0x00000021 => "Server Foundation",
			0x000000AF => "Enterprise for Virtual Desktops",
			0x00000009 => "Small Business Server",
			0x00000019 => "Small Business Server Premium",
			0x0000003F => "Small Business Server Premium (core installation)",
			0x00000038 => "MultiPoint Server",
			0x0000004F => "Server Standard (evaluation installation)",
			0x00000007 => "Server Standard (full installation)",
			0x0000000D => "Server Standard (core installation)",
			0x00000028 => "Server Standard without Hyper-V (core installation)",
			0x00000024 => "Server Standard without Hyper-V",
			0x00000034 => "Server Solutions Premium",
			0x00000035 => "Server Solutions Premium (core installation)",
			0x0000000B => "Starter",
			0x00000042 => "Not supported",
			0x0000002F => "Starter N",
			0x00000017 => "Storage Server Enterprise",
			0x0000002E => "Storage Server Enterprise (core installation)",
			0x00000014 => "Storage Server Express",
			0x0000002B => "Storage Server Express (core installation)",
			0x00000060 => "Storage Server Standard (evaluation installation)",
			0x00000015 => "Storage Server Standard",
			0x0000002C => "Storage Server Standard (core installation)",
			0x0000005F => "Storage Server Workgroup (evaluation installation)",
			0x00000016 => "Storage Server Workgroup",
			0x0000002D => "Storage Server Workgroup (core installation)",
			0x00000001 => "Ultimate",
			0x00000047 => "Not supported",
			0x0000001C => "Ultimate N",
			0x00000000 => "An unknown product",
			0x00000011 => "Web Server (full installation)",
			0x0000001D => "Web Server (core installation)",
			_ => $"0x{pdwReturnedProductType:X8}"
		};
	}

	private static string? GetHklmString(string key, string value)
	{
		const uint sz = 0x00000002;

		const int bufferSize = 8;
		ushort* data = stackalloc ushort[bufferSize];
		return GetRegistryValue(key, value, sz, data, bufferSize * sizeof(ushort)) ? null : new string((char*) data);
	}

	private static bool TryGetHklmDword(string key, string value, out uint result)
	{
		const uint dword = 0x00000018;

		uint data = 0;
		bool ret = GetRegistryValue(key, value, dword, &data, sizeof(uint));
		result = data;
		return ret;
	}

	/// <summary>
	/// Checks whether Windows provides accurate architecture info.
	/// </summary>
	/// <param name="processArchitecture">Process architecture.</param>
	/// <param name="systemArchitecture">System architecture.</param>
	/// <returns>Whether Windows provides accurate architecture info.</returns>
	internal static bool TryGetWindowsArchitecture(out Architecture processArchitecture, out Architecture systemArchitecture)
	{
		processArchitecture = 0;
		systemArchitecture = 0;

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return false;

		try
		{
			ushort processArch = 0;
			ushort systemArch = 0;
			if (GetArchitecture(GetCurrentProcess(), &processArch, &systemArch) != 0)
			{
				systemArchitecture = FromWindowsArch(systemArch);
				processArchitecture = processArch == 0 ? systemArchitecture : FromWindowsArch(processArch);
				return true;
			}
		}
		catch
		{
			// ignore
		}

		return false;

		static Architecture FromWindowsArch(ushort arch)
		{
			// from https://learn.microsoft.com/en-us/windows/win32/sysinfo/image-file-machine-constants
			return arch switch
			{
				0x014c => Architecture.X86,
				0x01c0 or 0x01c2 or 0x01c4 => Architecture.Arm,
				0x01F0 => (Architecture) 8, // PowerPC LE
				0x8664 => Architecture.X64,
				0xAA64 => Architecture.Arm64,
				_ => throw new PlatformNotSupportedException()
			};
		}
	}
}
