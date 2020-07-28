using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NorthwoodLib
{
	public static class OperatingSystem
	{
#pragma warning disable IDE1006
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

		[DllImport("ntdll")]
		private static extern uint RtlGetVersion(ref OSVERSIONINFO versionInfo);

		[DllImport("ntdll")]
		private static extern int RtlNtStatusToDosError(uint status);

		[DllImport("user32")]
		private static extern int GetSystemMetrics(int id);

		public static readonly bool UsesNativeData;
		public static readonly Version Version;
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
			VersionString = $"Windows {ProcessWindowsVersion(osVersionInfo)} {osVersionInfo.dwMajorVersion}.{osVersionInfo.dwMinorVersion}.{osVersionInfo.dwBuildNumber}";
			if (osVersionInfo.wServicePackMajor != 0 || osVersionInfo.wServicePackMinor != 0)
				VersionString += $"-{osVersionInfo.wServicePackMajor}.{osVersionInfo.wServicePackMinor}";
			if (!string.IsNullOrWhiteSpace(osVersionInfo.szCSDVersion))
				VersionString += $" {osVersionInfo.szCSDVersion}";
			VersionString += $" {(Environment.Is64BitOperatingSystem ? "64bit" : "32bit")} Process: {IntPtr.Size * 8}bit";
			VersionString = VersionString.Trim();
			// ReSharper restore HeapView.BoxingAllocation
		}

		private static string ProcessWindowsVersion(OSVERSIONINFO version)
		{
			if (version.wProductType == 1)
				switch (version.dwMajorVersion)
				{
					case 10:
						switch (version.dwMinorVersion)
						{
							case 0: return "10";
						}
						break;
					case 6:
						switch (version.dwMinorVersion)
						{
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
						switch (version.dwMinorVersion)
						{
							case 2 when GetSystemMetrics(89) != 0: return "Server 2003 R2";
							case 2: return "Server 2003";
						}
						break;
				}

			return "Unknown";
		}
	}
}
