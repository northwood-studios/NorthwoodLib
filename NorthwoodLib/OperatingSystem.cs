using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using NorthwoodLib.Logging;

namespace NorthwoodLib
{
	/// <summary>
	/// Provides data about currently used Operating System
	/// </summary>
	public static class OperatingSystem
	{
#pragma warning disable IDE1006
		/// <summary>
		/// Managed version of https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct OSVERSIONINFO
		{
			// The dwOSVersionInfoSize field must be set to Marshal.SizeOf<OSVERSIONINFO>()
			internal uint dwOSVersionInfoSize;
			internal readonly uint dwMajorVersion;
			internal readonly uint dwMinorVersion;
			internal readonly uint dwBuildNumber;
			private readonly uint dwPlatformId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			internal readonly string szCSDVersion;
			internal readonly ushort wServicePackMajor;
			internal readonly ushort wServicePackMinor;
			private readonly ushort wSuiteMask;
			internal readonly byte wProductType;
			private readonly byte wReserved;
		}
#pragma warning restore IDE1006

		private const string Ntdll = "ntdll";
		private const string User32 = "user32";
		private const string Kernel32 = "kernel32";

		// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
		/// <summary>
		/// Returns used Wine version https://wiki.winehq.org/Developer_FAQ#How_can_I_detect_Wine.3F
		/// </summary>
		/// <returns>Used Wine version</returns>
		[DllImport(Ntdll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "wine_get_version")]
		private static extern string GetWineVersion();

		/// <summary>
		/// Returns version information about the currently running operating system. https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion
		/// </summary>
		/// <param name="lpVersionInformation"><see cref="OSVERSIONINFO"/> that contains the version information about the currently running operating system.</param>
		/// <returns><see cref="RtlGetVersion"/> returns STATUS_SUCCESS.</returns>
		[DllImport(Ntdll)]
		private static extern uint RtlGetVersion(ref OSVERSIONINFO lpVersionInformation);

		/// <summary>
		/// Converts the specified NTSTATUS code to its equivalent system error code. https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-rtlntstatustodoserror
		/// </summary>
		/// <param name="Status">The NTSTATUS code to be converted.</param>
		/// <returns>Corresponding system error code.</returns>
		[DllImport(Ntdll)]
		private static extern int RtlNtStatusToDosError(uint Status);

		/// <summary>
		/// Retrieves the specified system metric or system configuration setting. https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics
		/// </summary>
		/// <param name="nIndex">The system metric or configuration setting to be retrieved.</param>
		/// <returns>Requested system metric or configuration setting.</returns>
		[DllImport(User32)]
		private static extern int GetSystemMetrics(int nIndex);

		/// <summary>
		/// Retrieves the product type for the operating system on the local computer, and maps the type to the product types supported by the specified operating system. https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo
		/// </summary>
		/// <param name="idwOSMajorVersiond">The major version number of the operating system.</param>
		/// <param name="dwOSMinorVersion">The minor version number of the operating system.</param>
		/// <param name="dwSpMajorVersion">The major version number of the operating system service pack.</param>
		/// <param name="dwSpMinorVersion">The minor version number of the operating system service pack.</param>
		/// <param name="pdwReturnedProductType">The product type.</param>
		/// <returns>A nonzero value on success. This function fails if one of the input parameters is invalid.</returns>
		[DllImport(Kernel32)]
		private static extern bool GetProductInfo(uint idwOSMajorVersiond, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out uint pdwReturnedProductType);
#pragma warning restore IDE1006
		// ReSharper restore InconsistentNaming

		/// <summary>
		/// Informs if code uses P/Invokes to obtain the data
		/// </summary>
		public static readonly bool UsesNativeData;
		/// <summary>
		/// Used Operating System <see cref="System.Version"/>
		/// </summary>
		public static readonly Version Version;
		/// <summary>
		/// Returns human readable description of the used Used Operating System
		/// </summary>
		public static readonly string VersionString;

		static OperatingSystem()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Version = Environment.OSVersion.Version;
				VersionString = $"{Environment.OSVersion.VersionString} {Environment.OSVersion.ServicePack}".Trim();
				UsesNativeData = false;
				return;
			}

			VersionString = "";
			try
			{
				string wineVersion = GetWineVersion();
				if (!string.IsNullOrWhiteSpace(wineVersion))
					VersionString += $"Wine {wineVersion} ";
				PlatformSettings.Log($"Wine {wineVersion} detected!", LogType.Info);
			}
			catch (Exception ex)
			{
				// not using wine, ignore
				PlatformSettings.Log($"Wine not detected! {ex.Message}", LogType.Debug);
			}

			OSVERSIONINFO osVersionInfo = new OSVERSIONINFO
			{
				dwOSVersionInfoSize = (uint) Marshal.SizeOf<OSVERSIONINFO>()
			};
			uint status = RtlGetVersion(ref osVersionInfo);
			if (status != 0)
				throw new Win32Exception(RtlNtStatusToDosError(status));

			UsesNativeData = true;
			Version = new Version((int) osVersionInfo.dwMajorVersion, (int) osVersionInfo.dwMinorVersion, (int) osVersionInfo.dwBuildNumber);

			// ReSharper disable HeapView.BoxingAllocation
			// pass by ref to avoid huge struct copiesk
			VersionString += $"Windows {ProcessWindowsVersion(ref osVersionInfo)}";
			string product = GetProductInfo(ref osVersionInfo);
			if (!string.IsNullOrWhiteSpace(product))
				VersionString += $" {product}";
			VersionString += $" {osVersionInfo.dwMajorVersion}.{osVersionInfo.dwMinorVersion}.{osVersionInfo.dwBuildNumber}";
			if (osVersionInfo.wServicePackMajor != 0 || osVersionInfo.wServicePackMinor != 0)
				VersionString += $"-{osVersionInfo.wServicePackMajor}.{osVersionInfo.wServicePackMinor}";
			if (!string.IsNullOrWhiteSpace(osVersionInfo.szCSDVersion))
				VersionString += $" {osVersionInfo.szCSDVersion}";

			int systemBits = Environment.Is64BitOperatingSystem ? 64 : 32;
			int processBits = IntPtr.Size * 8;
			if (systemBits == processBits)
				VersionString += $" {systemBits}bit";
			else
				VersionString += $" {systemBits}bit Process: {processBits}bit";
			VersionString = VersionString.Trim();
			// ReSharper restore HeapView.BoxingAllocation
		}

		private static string ProcessWindowsVersion(ref OSVERSIONINFO version)
		{
			// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006
			const byte VER_NT_WORKSTATION = 1;
#pragma warning restore IDE1006
			// from https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
			if (version.wProductType == VER_NT_WORKSTATION)
				switch (version.dwMajorVersion)
				{
					case 10:
						switch (version.dwMinorVersion)
						{
							case 0 when version.dwBuildNumber == 10240: return "10 1507";
							case 0 when version.dwBuildNumber == 10586: return "10 1511";
							case 0 when version.dwBuildNumber == 14393: return "10 1607";
							case 0 when version.dwBuildNumber == 15063: return "10 1703";
							case 0 when version.dwBuildNumber == 16299: return "10 1709";
							case 0 when version.dwBuildNumber == 17134: return "10 1803";
							case 0 when version.dwBuildNumber == 17763: return "10 1809";
							case 0 when version.dwBuildNumber == 18362: return "10 1903";
							case 0 when version.dwBuildNumber == 18363: return "10 1909";
							case 0 when version.dwBuildNumber == 19041: return "10 2004";
							case 0 when version.dwBuildNumber == 19042: return "10 20H2";
							case 0 when version.dwBuildNumber > 19042: return "10 Dev Channel";
							case 0: return "10 Preview";
						}
						break;
					case 6:
						switch (version.dwMinorVersion)
						{
							case 4: return "10 Prerelease";
							case 3: return "8.1";
							case 2: return "8";
							case 1: return "7";
							case 0: return "Vista";
						}
						break;
					case 5:
						switch (version.dwMinorVersion)
						{
							case 2: return "XP Professional x64 Edition";
							case 1: return "XP";
							case 0: return "2000";
						}
						break;
				}
			else
				switch (version.dwMajorVersion)
				{
					case 10:
						switch (version.dwMinorVersion)
						{
							case 0 when version.dwBuildNumber >= 17677: return "Server 2019";
							case 0: return "Server 2016";
						}
						break;
					case 6:
						switch (version.dwMinorVersion)
						{
							case 3: return "Server 2012 R2";
							case 2: return "Server 2012";
							case 1: return "Server 2008 R2";
							case 0: return "Server 2008";
						}
						break;
					case 5:
						// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006
						// from https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics
						const int SM_SERVERR2 = 89;
#pragma warning restore IDE1006
						switch (version.dwMinorVersion)
						{
							case 2 when GetSystemMetrics(SM_SERVERR2) != 0: return "Server 2003 R2";
							case 2: return "Server 2003";
						}
						break;
				}

			return "Unknown";
		}

		private static string GetProductInfo(ref OSVERSIONINFO version)
		{
			if (version.dwMajorVersion < 6 || !GetProductInfo(version.dwMajorVersion, version.dwMinorVersion,
				version.wServicePackMajor, version.wServicePackMinor, out uint pdwReturnedProductType))
				return null;
			// from https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo
			switch (pdwReturnedProductType)
			{
				case 0x00000006: return "Business";
				case 0x00000010: return "Business N";
				case 0x00000012: return "HPC Edition";
				case 0x00000040: return "Server Hyper Core V";
				case 0x00000065: return "Home";
				case 0x00000063: return "Home China";
				case 0x00000062: return "Home N";
				case 0x00000064: return "Home Single Language";
				case 0x00000050: return "Server Datacenter (evaluation installation)";
				case 0x00000091: return "Server Datacenter, Semi-Annual Channel (core installation)";
				case 0x00000092: return "Server Standard, Semi-Annual Channel (core installation)";
				case 0x00000008: return "Server Datacenter (full installation)";
				case 0x0000000C: return "Server Datacenter (core installation)";
				case 0x00000027: return "Server Datacenter without Hyper-V (core installation)";
				case 0x00000025: return "Server Datacenter without Hyper-V (full installation)";
				case 0x00000079: return "Education";
				case 0x0000007A: return "Education N";
				case 0x00000004: return "Enterprise";
				case 0x00000046: return "Enterprise E";
				case 0x00000048: return "Enterprise Evaluation";
				case 0x0000001B: return "Enterprise N";
				case 0x00000054: return "Enterprise N Evaluation";
				case 0x0000007D: return "Enterprise 2015 LTSB";
				case 0x00000081: return "Enterprise 2015 LTSB Evaluation";
				case 0x0000007E: return "Enterprise 2015 LTSB N";
				case 0x00000082: return "Enterprise 2015 LTSB N Evaluation";
				case 0x0000000A: return "Server Enterprise (full installation)";
				case 0x0000000E: return "Server Enterprise (core installation)";
				case 0x00000029: return "Server Enterprise without Hyper-V (core installation)";
				case 0x0000000F: return "Server Enterprise for Itanium-based Systems";
				case 0x00000026: return "Server Enterprise without Hyper-V (full installation)";
				case 0x0000003C: return "Essential Server Solution Additional";
				case 0x0000003E: return "Essential Server Solution Additional SVC";
				case 0x0000003B: return "Essential Server Solution Management";
				case 0x0000003D: return "Essential Server Solution Management SVC";
				case 0x00000002: return "Home Basic";
				case 0x00000043: return "Not supported";
				case 0x00000005: return "Home Basic N";
				case 0x00000003: return "Home Premium";
				case 0x00000044: return "Not supported";
				case 0x0000001A: return "Home Premium N";
				case 0x00000022: return "Home Server 2011";
				case 0x00000013: return "Storage Server 2008 R2 Essentials";
				case 0x0000002A: return "Microsoft Hyper-V Server";
				case 0x0000007B: return "IoT Core";
				case 0x00000083: return "IoT Core Commercial";
				case 0x0000001E: return "Essential Business Server Management Server";
				case 0x00000020: return "Essential Business Server Messaging Server";
				case 0x0000001F: return "Essential Business Server Security Server";
				case 0x00000068: return "Mobile";
				case 0x00000085: return "Mobile Enterprise";
				case 0x0000004D: return "MultiPoint Server Premium (full installation)";
				case 0x0000004C: return "MultiPoint Server Standard (full installation)";
				case 0x000000A1: return "Pro for Workstations";
				case 0x000000A2: return "Pro for Workstations N";
				case 0x00000030: return "Pro";
				case 0x00000045: return "Not supported";
				case 0x00000031: return "Pro N";
				case 0x00000067: return "Professional with Media Center";
				case 0x00000032: return "Small Business Server 2011 Essentials";
				case 0x00000036: return "Server For SB Solutions EM";
				case 0x00000033: return "Server For SB Solutions";
				case 0x00000037: return "Server For SB Solutions EM";
				case 0x00000018: return "Server 2008 for Windows Essential Server Solutions";
				case 0x00000023: return "Server 2008 without Hyper-V for Windows Essential Server Solutions";
				case 0x00000021: return "Server Foundation";
				case 0x00000009: return "Small Business Server";
				case 0x00000019: return "Small Business Server Premium";
				case 0x0000003F: return "Small Business Server Premium (core installation)";
				case 0x00000038: return "MultiPoint Server";
				case 0x0000004F: return "Server Standard (evaluation installation)";
				case 0x00000007: return "Server Standard (full installation)";
				case 0x0000000D: return "Server Standard (core installation)";
				case 0x00000028: return "Server Standard without Hyper-V (core installation)";
				case 0x00000024: return "Server Standard without Hyper-V";
				case 0x00000034: return "Server Solutions Premium";
				case 0x00000035: return "Server Solutions Premium (core installation)";
				case 0x0000000B: return "Starter";
				case 0x00000042: return "Not supported";
				case 0x0000002F: return "Starter N";
				case 0x00000017: return "Storage Server Enterprise";
				case 0x0000002E: return "Storage Server Enterprise (core installation)";
				case 0x00000014: return "Storage Server Express";
				case 0x0000002B: return "Storage Server Express (core installation)";
				case 0x00000060: return "Storage Server Standard (evaluation installation)";
				case 0x00000015: return "Storage Server Standard";
				case 0x0000002C: return "Storage Server Standard (core installation)";
				case 0x0000005F: return "Storage Server Workgroup (evaluation installation)";
				case 0x00000016: return "Storage Server Workgroup";
				case 0x0000002D: return "Storage Server Workgroup (core installation)";
				case 0x00000001: return "Ultimate";
				case 0x00000047: return "Not supported";
				case 0x0000001C: return "Ultimate N";
				case 0x00000000: return "An unknown product";
				case 0x00000011: return "Web Server (full installation)";
				case 0x0000001D: return "Web Server (core installation)";
				// ReSharper disable once HeapView.BoxingAllocation
				default: return $"0x{pdwReturnedProductType:X8}";
			}
		}
	}
}
