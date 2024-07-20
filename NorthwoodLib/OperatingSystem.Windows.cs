using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NorthwoodLib.Logging;
using NorthwoodLib.Pools;

namespace NorthwoodLib;

public static unsafe partial class OperatingSystem
{
	// ReSharper disable InconsistentNaming
	// ReSharper disable FieldCanBeMadeReadOnly.Local
#pragma warning disable IDE1006
#pragma warning disable IDE0044
	/// <summary>
	/// Managed version of <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw"/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct OSVERSIONINFO
	{
		/// <summary>
		/// The marshalled size, in bytes, of an <see cref="OSVERSIONINFO"/> structure.
		/// This member must be set to <see cref="Marshal.SizeOf{OSVERSIONINFO}()"/> before the structure is used with <see cref="GetVersion"/>.
		/// </summary>
		public uint dwOSVersionInfoSize;

		/// <summary>
		/// The major version number of the operating system.
		/// </summary>
		public uint dwMajorVersion;

		/// <summary>
		/// The minor version number of the operating system.
		/// </summary>
		public uint dwMinorVersion;

		/// <summary>
		/// The build number of the operating system.
		/// </summary>
		public uint dwBuildNumber;

		/// <summary>
		/// The operating system platform. For Win32 on NT-based operating systems, RtlGetVersion returns the value VER_PLATFORM_WIN32_NT.
		/// </summary>
		private uint dwPlatformId;

		/// <summary>
		/// The service-pack version string.
		/// </summary>
		public fixed ushort szCSDVersion[128];

		/// <summary>
		/// The major version number of the latest service pack installed on the system.
		/// </summary>
		public ushort wServicePackMajor;

		/// <summary>
		/// The minor version number of the latest service pack installed on the system.
		/// </summary>
		public ushort wServicePackMinor;

		/// <summary>
		/// The product suites available on the system.
		/// </summary>
		private ushort wSuiteMask;

		/// <summary>
		/// The product type.
		/// </summary>
		public byte wProductType;

		/// <summary>
		/// Reserved for future use.
		/// </summary>
		private byte wReserved;
	}
#pragma warning restore IDE0044
	// ReSharper restore FieldCanBeMadeReadOnly.Local

	private const string Ntdll = "ntdll";
	private const string Kernel32 = "kernel32";
	private const string RegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

	/// <summary>
	/// Returns version information about the currently running operating system.
	/// <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion"/>
	/// </summary>
	/// <param name="lpVersionInformation"><see cref="OSVERSIONINFO"/> that contains the version information about the currently running operating system.</param>
	/// <returns><see cref="GetVersion"/> returns STATUS_SUCCESS.</returns>
	[DllImport(Ntdll, EntryPoint = "RtlGetVersion", ExactSpelling = true)]
	private static extern uint GetVersion(OSVERSIONINFO* lpVersionInformation);

	/// <summary>
	/// Converts the specified NTSTATUS code to its equivalent system error code.
	/// <see href="https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-rtlntstatustodoserror"/>
	/// </summary>
	/// <param name="status">The NTSTATUS code to be converted.</param>
	/// <returns>Corresponding system error code.</returns>
	[DllImport(Ntdll, EntryPoint = "RtlNtStatusToDosError", ExactSpelling = true)]
	private static extern int NtStatusToDosCode(uint status);

	/// <summary>
	/// Returns version information about the currently running operating system.
	/// <see href="https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getversionexw"/>
	/// </summary>
	/// <param name="lpVersionInformation"><see cref="OSVERSIONINFO"/> that contains the version information about the currently running operating system.</param>
	/// <returns>True on success</returns>
	[DllImport(Kernel32, EntryPoint = "GetVersionExW", ExactSpelling = true, SetLastError = true)]
	private static extern uint GetVersionFallback(OSVERSIONINFO* lpVersionInformation);

	/// <summary>
	/// Retrieves the product type for the operating system on the local computer, and maps the type to the product types supported by the specified operating system.
	/// <see href="https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo"/>
	/// </summary>
	/// <param name="idwOSMajorVersion">The major version number of the operating system.</param>
	/// <param name="dwOSMinorVersion">The minor version number of the operating system.</param>
	/// <param name="dwSpMajorVersion">The major version number of the operating system service pack.</param>
	/// <param name="dwSpMinorVersion">The minor version number of the operating system service pack.</param>
	/// <param name="pdwReturnedProductType">The product type.</param>
	/// <returns>A nonzero value on success. This function fails if one of the input parameters is invalid.</returns>
	[DllImport(Kernel32, EntryPoint = "GetProductInfo", ExactSpelling = true)]
	private static extern uint GetProductInfo(uint idwOSMajorVersion, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, uint* pdwReturnedProductType);
#pragma warning restore IDE1006
	// ReSharper restore InconsistentNaming

	private static bool TryGetWindowsVersion(out Version version, out string name)
	{
		StringBuilder nameBuilder = StringBuilderPool.Shared.Rent(WineInfo.UsesWine ? $"{WineInfo.WineVersion} " : "");
		version = null;

		bool server = false;
		string servicePack = null;
		Version servicePackVersion = null;

		OSVERSIONINFO osVersionInfo = new()
		{
			dwOSVersionInfoSize = (uint) sizeof(OSVERSIONINFO)
		};

		try
		{
			uint status = GetVersion(&osVersionInfo);
			if (status != 0)
				throw new Win32Exception(NtStatusToDosCode(status));
			ParseWindowsVersion(osVersionInfo, out version, out server, out servicePack, out servicePackVersion);

		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Failed to get Windows version! {ex.Message}", LogType.Warning);
		}

		if (!IsValidWindowsVersion(version))
			try
			{
				if (GetVersionFallback(&osVersionInfo) == 0)
					throw new Win32Exception();
				ParseWindowsVersion(osVersionInfo, out version, out server, out servicePack, out servicePackVersion);
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Failed to correct Windows version with GetVersionExW! {ex.Message}", LogType.Warning);
			}

		if (!IsValidWindowsVersion(version))
			try
			{
				if (TryCheckWindowsFileVersion(out Version fileVersion, GetWindowsRegistryBuild()))
				{
					PlatformSettings.Log(
						$"Correcting system version using files from {version.PrintVersion()} to {fileVersion.Major}.{fileVersion.Minor}.{fileVersion.Build}",
						LogType.Warning);
					version = fileVersion;
				}
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Failed to correct Windows version using files! {ex.Message}", LogType.Warning);
			}

		if (!IsValidWindowsVersion(version))
			version = Environment.OSVersion.Version;

		if (string.IsNullOrWhiteSpace(servicePack))
			servicePack = Environment.OSVersion.ServicePack;

		servicePackVersion ??= Environment.OSVersion.ServicePack switch
		{
			"Service Pack 1" => new Version(1, 0),
			"Service Pack 2" => new Version(2, 0),
			"Service Pack 3" => new Version(3, 0),
			"Service Pack 4" => new Version(4, 0),
			"Service Pack 5" => new Version(5, 0),
			_ => null
		};

		// ReSharper disable HeapView.BoxingAllocation
		nameBuilder.Append($"Windows {ProcessWindowsVersion(version, server, GetHklmString(RegistryPath, "DisplayVersion"))}");

		string product = GetProductInfo(version, servicePackVersion);

		if (!string.IsNullOrWhiteSpace(product))
			nameBuilder.Append($" {product}");

		if (!string.IsNullOrWhiteSpace(servicePack))
			nameBuilder.Append($" {servicePack}");

		int systemBits = Environment.Is64BitOperatingSystem ? 64 : 32;
		int processBits = sizeof(nuint) * 8;

		nameBuilder.Append(systemBits == processBits ?
			$" {systemBits}bit" :
			$" {systemBits}bit Process: {processBits}bit");

		name = StringBuilderPool.Shared.ToStringReturn(nameBuilder).Trim();
		// ReSharper restore HeapView.BoxingAllocation
		return true;
	}

	private static void ParseWindowsVersion(OSVERSIONINFO osVersionInfo, out Version version, out bool server, out string servicePack, out Version servicePackVersion)
	{
		version = new Version((int) osVersionInfo.dwMajorVersion, (int) osVersionInfo.dwMinorVersion, (int) osVersionInfo.dwBuildNumber);
		// from https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
		server = osVersionInfo.wProductType != 1;
		servicePack = new string((char*) osVersionInfo.szCSDVersion);
		servicePackVersion = new Version(osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor);
	}

	/// <summary>
	/// Attempts to retrieve the Windows Build from registry
	/// </summary>
	/// <returns>Parsed value if successful, <see langword="null"/> otherwise</returns>
	internal static int? GetWindowsRegistryBuild()
	{
		return int.TryParse(GetHklmString(RegistryPath, "CurrentBuildNumber"), out int build) && build > 0
			? build
			: int.TryParse(GetHklmString(RegistryPath, "CurrentBuild"), out build) && build > 0
				? build
				: null;
	}

	/// <summary>
	/// Fetches the Windows version from system files
	/// </summary>
	/// <param name="version">System version</param>
	/// <param name="realBuild">Real build value if known</param>
	/// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise</returns>
	internal static bool TryCheckWindowsFileVersion(out Version version, int? realBuild)
	{
		foreach (string file in (ReadOnlySpan<string>) [
					 "cmd.exe",
					 "conhost.exe",
					 "dxdiag.exe",
					 "msinfo32.exe",
					 "msconfig.exe",
					 "mspaint.exe",
					 "notepad.exe",
					 "winver.exe"
				 ])
		{
			FileVersionInfo fileVersionInfo;

			try
			{
				// use a system file to obtain the true version
				fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.SystemDirectory, file));
			}
			catch (FileNotFoundException)
			{
				continue;
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Failed to get the version of {file}! {ex.Message}", LogType.Warning);
				continue;
			}

			version = new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, realBuild ?? fileVersionInfo.FileBuildPart);

			if (!IsValidWindowsVersion(version))
				version = new Version(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, realBuild ?? fileVersionInfo.ProductBuildPart);

			if (IsValidWindowsVersion(version))
				return true;
		}

		version = null;
		return false;
	}

	private static bool IsValidWindowsVersion(Version version)
	{
		return version != null && IsValidWindowsVersion(version.Major, version.Minor);
	}

	private static bool IsValidWindowsVersion(int major, int minor)
	{
		// 6.2 is Windows 8, Windows 8.1 and 10 sometimes like to identify themselves as it
		return major > 0 && minor >= 0 && !(major == 6 && minor == 2);
	}

	private static string PrintVersion(this Version version)
	{
		return version == null ? "(null)" : $"{version.Major}.{version.Minor}{(version.Build > 0 ? $".{version.Build}" : "")}";
	}

	private static string ProcessWindowsVersion(Version version, bool server, string displayVersion)
	{
		switch (version?.Major)
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
						14393 => "1607",
						16299 => "1709",
						17134 => "1803",
						17763 => "1809",
						18362 => "1903",
						18363 => "1909",
						19041 => "2004",
						19042 => "20H2",
						20348 => "21H2",
						25398 => "23H2",
						26040 => "23H2",
						26100 => "24H2",
						_ => $"build {version.Build}"
					};
					return $"Server {main} {patch}";
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
						10240 => "1507",
						10586 => "1511",
						14393 => "1607",
						15063 => "1703",
						16299 => "1709",
						17134 => "1803",
						17763 => "1809",
						18362 => "1903",
						18363 => "1909",
						19041 => "2004",
						19042 => "20H2",
						19043 => "21H1",
						19044 => "21H2",
						19045 => "22H2",
						22000 => "21H2",
						22621 => "22H2",
						22631 => "23H2",
						26100 => "24H2",
						_ => $"build {version.Build}"
					};
					return $"{main} {patch}";
				}
			case 6 when server:
				{
					switch (version.Minor)
					{
						case 3: return "Server 2012 R2";
						case 2: return "Server 2012";
						case 1: return "Server 2008 R2";
						case 0: return "Server 2008";
					}
					break;
				}
			case 6:
				{
					switch (version.Minor)
					{
						case 3: return "8.1";
						case 2: return "8";
						case 1: return "7";
						case 0: return "Vista";
					}
					break;
				}
		}

		return version.PrintVersion();
	}

	private static string GetProductInfo(Version osVersion, Version spVersion)
	{
		uint pdwReturnedProductType = 0;

		try
		{
			if (GetProductInfo((uint) osVersion.Major, (uint) osVersion.Minor,
					(uint) spVersion.Major, (uint) spVersion.Minor, &pdwReturnedProductType) == 0)
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

	private static string GetHklmString(string key, string value)
	{
		[DllImport("Advapi32", EntryPoint = "RegGetValueW", ExactSpelling = true)]
		static extern int RegGetValue(nint hkey, ushort* key, ushort* value, uint flags, uint* type, void* data, uint* dataLength);

		const nint hklm = -2147483646;
		const uint sz = 0x00000002;

		const int bufferSize = 8;
		ushort* data = stackalloc ushort[bufferSize];

		uint dataSize = bufferSize * sizeof(ushort);
		fixed (char* keyPointer = key)
		fixed (char* valuePointer = value)
		{
			if (RegGetValue(hklm, (ushort*) keyPointer, (ushort*) valuePointer, sz, null, data, &dataSize) != 0)
				return null;
		}

		return new string((char*) data, 0, (int)(dataSize / sizeof(ushort) - 1));
	}
}
