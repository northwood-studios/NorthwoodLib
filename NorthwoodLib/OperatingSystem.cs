using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
// ReSharper disable HeapView.BoxingAllocation

namespace NorthwoodLib
{
	public static class OperatingSystem
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct OSVERSIONINFO
		{
			// The OSVersionInfoSize field must be set to Marshal.SizeOf<OSVERSIONINFO>()
			internal uint OSVersionInfoSize;
			internal readonly uint MajorVersion;
			internal readonly uint MinorVersion;
			internal readonly uint BuildNumber;
			private readonly uint PlatformId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			internal readonly string CSDVersion;
		}

		[DllImport("ntdll.dll")]
		private static extern uint RtlGetVersion(ref OSVERSIONINFO versionInfo);

		[DllImport("ntdll.dll")]
		private static extern int RtlNtStatusToDosError(uint status);

		internal static readonly bool UseNativeData;
		internal static readonly Version NativeVersion;
		internal static readonly string NativeVersionString;

		static OperatingSystem()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				UseNativeData = false;
				return;
			}
			OSVERSIONINFO osVersionInfo = new OSVERSIONINFO
			{
				OSVersionInfoSize = (uint) Marshal.SizeOf<OSVERSIONINFO>()
			};
			uint status = RtlGetVersion(ref osVersionInfo);
			if (status != 0)
				throw new Win32Exception(RtlNtStatusToDosError(status));
			UseNativeData = true;
			NativeVersion = new Version((int) osVersionInfo.MajorVersion, (int) osVersionInfo.MinorVersion, (int) osVersionInfo.BuildNumber);
			NativeVersionString = $"Windows {osVersionInfo.MajorVersion}.{osVersionInfo.MinorVersion}.{osVersionInfo.BuildNumber}";
			if (!string.IsNullOrEmpty(osVersionInfo.CSDVersion))
				NativeVersionString += " " + osVersionInfo.CSDVersion;
		}

		public static string GetSystemVersionString()
		{
			return UseNativeData ? NativeVersionString : Environment.OSVersion.VersionString;
		}

		public static Version GetSystemVersion()
		{
			return UseNativeData ? NativeVersion : Environment.OSVersion.Version;
		}
	}
}
