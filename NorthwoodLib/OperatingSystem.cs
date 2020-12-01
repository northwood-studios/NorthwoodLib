using NorthwoodLib.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace NorthwoodLib
{
	/// <summary>
	/// Provides data about currently used Operating System
	/// </summary>
	public static class OperatingSystem
	{
		// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
		/// <summary>
		/// Managed version of <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw"/>
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct OSVERSIONINFO
		{
			/// <summary>
			/// The marshalled size, in bytes, of an <see cref="OSVERSIONINFO"/> structure. This member must be set to <see cref="Marshal.SizeOf{OSVERSIONINFO}()"/> before the structure is used with <see cref="GetVersion"/>.
			/// </summary>
			public uint dwOSVersionInfoSize;
			/// <summary>
			/// The major version number of the operating system.
			/// </summary>
			public readonly uint dwMajorVersion;
			/// <summary>
			/// The minor version number of the operating system.
			/// </summary>
			public readonly uint dwMinorVersion;
			/// <summary>
			/// The build number of the operating system.
			/// </summary>
			public readonly uint dwBuildNumber;
			/// <summary>
			/// The operating system platform. For Win32 on NT-based operating systems, RtlGetVersion returns the value VER_PLATFORM_WIN32_NT.
			/// </summary>
			private readonly uint dwPlatformId;
			/// <summary>
			/// The service-pack version string.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public readonly string szCSDVersion;
			/// <summary>
			/// The major version number of the latest service pack installed on the system.
			/// </summary>
			public readonly ushort wServicePackMajor;
			/// <summary>
			/// The minor version number of the latest service pack installed on the system.
			/// </summary>
			public readonly ushort wServicePackMinor;
			/// <summary>
			/// The product suites available on the system.
			/// </summary>
			private readonly ushort wSuiteMask;
			/// <summary>
			/// The product type.
			/// </summary>
			public readonly byte wProductType;
			/// <summary>
			/// Reserved for future use.
			/// </summary>
			private readonly byte wReserved;
		}

		private const string Ntdll = "ntdll";
		private const string User32 = "user32";
		private const string Kernel32 = "kernel32";

		/// <summary>
		/// Returns version information about the currently running operating system. <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion"/>
		/// </summary>
		/// <param name="lpVersionInformation"><see cref="OSVERSIONINFO"/> that contains the version information about the currently running operating system.</param>
		/// <returns><see cref="GetVersion"/> returns STATUS_SUCCESS.</returns>
		[DllImport(Ntdll, EntryPoint = "RtlGetVersion", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern uint GetVersion(ref OSVERSIONINFO lpVersionInformation);

		/// <summary>
		/// Returns version information about the currently running operating system. <see href="https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getversionexw"/>
		/// </summary>
		/// <param name="lpVersionInformation"><see cref="OSVERSIONINFO"/> that contains the version information about the currently running operating system.</param>
		/// <returns>True on success</returns>
		[DllImport(Kernel32, EntryPoint = "GetVersionExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		private static extern bool GetVersionFallback(ref OSVERSIONINFO lpVersionInformation);

		/// <summary>
		/// Converts the specified NTSTATUS code to its equivalent system error code. <see href="https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-rtlntstatustodoserror"/>
		/// </summary>
		/// <param name="Status">The NTSTATUS code to be converted.</param>
		/// <returns>Corresponding system error code.</returns>
		[DllImport(Ntdll, EntryPoint = "RtlNtStatusToDosError", ExactSpelling = true)]
		private static extern int NtStatusToDosCode(uint Status);

		/// <summary>
		/// Retrieves the specified system metric or system configuration setting. <see href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics"/>
		/// </summary>
		/// <param name="nIndex">The system metric or configuration setting to be retrieved.</param>
		/// <returns>Requested system metric or configuration setting.</returns>
		[DllImport(User32, EntryPoint = "GetSystemMetrics", ExactSpelling = true)]
		private static extern int GetSystemMetrics(int nIndex);

		/// <summary>
		/// Retrieves the product type for the operating system on the local computer, and maps the type to the product types supported by the specified operating system. <see href="https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo"/>
		/// </summary>
		/// <param name="idwOSMajorVersiond">The major version number of the operating system.</param>
		/// <param name="dwOSMinorVersion">The minor version number of the operating system.</param>
		/// <param name="dwSpMajorVersion">The major version number of the operating system service pack.</param>
		/// <param name="dwSpMinorVersion">The minor version number of the operating system service pack.</param>
		/// <param name="pdwReturnedProductType">The product type.</param>
		/// <returns>A nonzero value on success. This function fails if one of the input parameters is invalid.</returns>
		[DllImport(Kernel32, EntryPoint = "GetProductInfo", ExactSpelling = true)]
		private static extern bool GetProductInfo(uint idwOSMajorVersiond, uint dwOSMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, out uint pdwReturnedProductType);
#pragma warning restore IDE1006
		// ReSharper restore InconsistentNaming

		/// <summary>
		/// Informs if code uses P/Invokes to obtain the data
		/// </summary>
		public static readonly bool UsesNativeData;
		/// <summary>
		/// Informs if user uses Wine. User can hide Wine so don't rely on this for uses other than diagnostic usage
		/// </summary>
		[Obsolete("Use WineInfo.UsesWine instead")]
		public static readonly bool UsesWine;
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
			UsesNativeData = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? TryGetWindowsVersion(out Version version, out string os) : TryGetUnixOs(out version, out os);
			Version = UsesNativeData ? version : Environment.OSVersion.Version;
			VersionString = UsesNativeData ? os : $"{Environment.OSVersion.VersionString} {Environment.OSVersion.ServicePack}".Trim();
#pragma warning disable 618
			UsesWine = WineInfo.UsesWine;
#pragma warning restore 618
		}

		private static bool TryGetWindowsVersion(out Version version, out string name)
		{
			name = WineInfo.UsesWine ? $"{WineInfo.WineVersion} " : "";
			version = null;

			bool server = false;
			string servicePack = null;
			Version servicePackVersion = null;

			try
			{
				OSVERSIONINFO osVersionInfo = new OSVERSIONINFO
				{
					dwOSVersionInfoSize = (uint) Marshal.SizeOf<OSVERSIONINFO>()
				};
				uint status = GetVersion(ref osVersionInfo);
				if (status != 0)
					throw new Win32Exception(NtStatusToDosCode(status));

				version = new Version((int) osVersionInfo.dwMajorVersion, (int) osVersionInfo.dwMinorVersion, (int) osVersionInfo.dwBuildNumber);
				// from https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
				server = osVersionInfo.wProductType != 1;
				servicePack = osVersionInfo.szCSDVersion;
				servicePackVersion = new Version(osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor);
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Failed to get Windows version! {ex.Message}", LogType.Warning);
			}

			if (!IsValidWindowsVersion(version))
				try
				{
					OSVERSIONINFO osVersionInfo = new OSVERSIONINFO
					{
						dwOSVersionInfoSize = (uint) Marshal.SizeOf<OSVERSIONINFO>()
					};
					if (!GetVersionFallback(ref osVersionInfo))
						throw new Win32Exception();

					version = new Version((int) osVersionInfo.dwMajorVersion, (int) osVersionInfo.dwMinorVersion, (int) osVersionInfo.dwBuildNumber);
					// from https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw
					server = osVersionInfo.wProductType != 1;
					servicePack = osVersionInfo.szCSDVersion;
					servicePackVersion = new Version(osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor);
				}
				catch (Exception ex)
				{
					PlatformSettings.Log($"Failed to correct Windows version with GetVersionExW! {ex.Message}", LogType.Warning);
				}

			try
			{
				if (!IsValidWindowsVersion(version) && TryCheckWindowsFileVersion(out Version fileVersion))
				{
					// ReSharper disable HeapView.BoxingAllocation
					PlatformSettings.Log($"Correcting system version using files from {version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0} to {fileVersion.Major}.{fileVersion.Minor}.{fileVersion.Build}", LogType.Warning);
					// ReSharper restore HeapView.BoxingAllocation
					version = fileVersion;
				}
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Failed to correct Windows version using files! {ex.Message}", LogType.Warning);
			}

			if (!IsValidWindowsVersion(version))
				version = Environment.OSVersion.Version;

			if (servicePack == null)
				servicePack = Environment.OSVersion.ServicePack;

			if (servicePackVersion == null)
				switch (Environment.OSVersion.ServicePack)
				{
					case "Service Pack 1":
						servicePackVersion = new Version(1, 0);
						break;
					case "Service Pack 2":
						servicePackVersion = new Version(2, 0);
						break;
					case "Service Pack 3":
						servicePackVersion = new Version(3, 0);
						break;
				}

			// ReSharper disable HeapView.BoxingAllocation
			name += $"Windows {ProcessWindowsVersion(version, server)}";
			string product = GetProductInfo(version, servicePackVersion);
			if (!string.IsNullOrWhiteSpace(product))
				name += $" {product}";
			name += $" {version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
			if (servicePackVersion != null && (servicePackVersion.Major != 0 || servicePackVersion.Minor != 0))
				name += $"-{servicePackVersion.Major}.{servicePackVersion.Minor}";
			if (!string.IsNullOrWhiteSpace(servicePack))
				name += $" {servicePack}";

			int systemBits = Environment.Is64BitOperatingSystem ? 64 : 32;
			int processBits = IntPtr.Size * 8;
			if (systemBits == processBits)
				name += $" {systemBits}bit";
			else
				name += $" {systemBits}bit Process: {processBits}bit";
			name = name.Trim();
			// ReSharper restore HeapView.BoxingAllocation
			return true;
		}

		/// <summary>
		/// Fetches the Windows version from system files
		/// </summary>
		/// <param name="version">System version</param>
		/// <returns>True if successful, false otherwise</returns>
		internal static bool TryCheckWindowsFileVersion(out Version version)
		{
			foreach (string file in new[] { "cmd.exe", "conhost.exe", "dxdiag.exe", "msinfo32.exe", "msconfig.exe", "mspaint.exe", "notepad.exe", "winver.exe" })
			{
				FileVersionInfo fileVersionInfo;
				try
				{
					// use a system file to obtain the true version
					string path = Path.Combine(Environment.SystemDirectory, file);
					if (!File.Exists(path))
						continue;
					fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
				}
				catch (Exception ex)
				{
					PlatformSettings.Log($"Failed to get the version of {file}! {ex.Message}", LogType.Warning);
					continue;
				}

				version = new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart);
				if (!IsValidWindowsVersion(version))
					version = new Version(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart);
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

		/// <summary>
		/// Checks /etc/os-release for Linux distribution info
		/// </summary>
		/// <param name="version">OS version</param>
		/// <param name="name">Used Linux distribution</param>
		/// <returns>True if operation was successful</returns>
		private static bool TryGetUnixOs(out Version version, out string name)
		{
			version = Environment.OSVersion.Version;
			// linux distributions store info about themselves here
			const string osrelease = "/etc/os-release";
			try
			{
				if (File.Exists(osrelease))
					using (FileStream fs = new FileStream(osrelease, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						using (StreamReader sr = new StreamReader(fs))
						{
							string line;
							while ((line = sr.ReadLine()) != null)
							{
								line = line.Trim();
								const string prettyname = "PRETTY_NAME=";
								if (line.StartsWith(prettyname))
								{
									name = $"{line.Substring(prettyname.Length).Replace("\"", "").Trim()} {Environment.OSVersion.VersionString}".Trim();
									return true;
								}
							}
						}
					}
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Error while reading {osrelease}: {ex.Message}", LogType.Warning);
			}

			name = null;
			return false;
		}

		private static string ProcessWindowsVersion(Version version, bool server)
		{
			if (server)
				switch (version.Major)
				{
					case 10:
						switch (version.Minor)
						{
							case 0 when version.Build >= 17677: return "Server 2019";
							case 0: return "Server 2016";
						}

						break;
					case 6:
						switch (version.Minor)
						{
							case 3: return "Server 2012 R2";
							case 2: return "Server 2012";
							case 1: return "Server 2008 R2";
							case 0: return "Server 2008";
						}

						break;
					case 5:
						try
						{
							switch (version.Minor)
							{
								// from https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics
								case 2 when GetSystemMetrics(89) != 0: return "Server 2003 R2";
								case 2: return "Server 2003";
							}
						}
						catch (Exception ex)
						{
							PlatformSettings.Log($"Error while fetching the Windows Server version: {ex.Message}", LogType.Warning);
							if (version.Minor == 2)
								return "Server 2003 - Error";
						}

						break;
				}
			else
				switch (version.Major)
				{
					case 10:
						switch (version.Minor)
						{
							case 0 when version.Build == 10240: return "10 1507";
							case 0 when version.Build == 10586: return "10 1511";
							case 0 when version.Build == 14393: return "10 1607";
							case 0 when version.Build == 15063: return "10 1703";
							case 0 when version.Build == 16299: return "10 1709";
							case 0 when version.Build == 17134: return "10 1803";
							case 0 when version.Build == 17763: return "10 1809";
							case 0 when version.Build == 18362: return "10 1903";
							case 0 when version.Build == 18363: return "10 1909";
							case 0 when version.Build == 19041: return "10 2004";
							case 0 when version.Build == 19042: return "10 20H2";
							case 0 when version.Build > 19042: return "10 Dev Channel";
							case 0: return "10 Preview";
						}

						break;
					case 6:
						switch (version.Minor)
						{
							case 4: return "10 Prerelease";
							case 3: return "8.1";
							case 2: return "8";
							case 1: return "7";
							case 0: return "Vista";
						}

						break;
					case 5:
						switch (version.Minor)
						{
							case 2: return "XP Professional x64 Edition";
							case 1: return "XP";
							case 0: return "2000";
						}

						break;
				}

			return "Unknown";
		}

		private static string GetProductInfo(Version osVersion, Version spVersion)
		{
			uint pdwReturnedProductType;
			try
			{
				if (!GetProductInfo((uint) osVersion.Major, (uint) osVersion.Minor,
					(uint) spVersion.Major, (uint) spVersion.Minor, out pdwReturnedProductType))
					return null;
			}
			catch (Exception ex)
			{
				PlatformSettings.Log($"Error while fetching the product info: {ex.Message}", LogType.Error);
				return null;
			}

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
