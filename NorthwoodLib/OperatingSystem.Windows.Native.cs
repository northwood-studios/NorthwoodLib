using System.Runtime.InteropServices;

namespace NorthwoodLib;

public static unsafe partial class OperatingSystem
{
	private const string Ntdll = "ntdll";
	private const string Kernel32 = "kernel32";
	private const string Advapi32 = "Advapi32";

	/// <summary>
	/// Returns version information about the currently running operating system.
	/// <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion"/>
	/// </summary>
	/// <param name="lpVersionInformation"><see cref="OsVersionInfo"/> that contains the version information about the currently running operating system.</param>
	/// <returns><see cref="GetVersion(OsVersionInfo*)"/> returns STATUS_SUCCESS.</returns>
	[DllImport(Ntdll, EntryPoint = "RtlGetVersion", ExactSpelling = true)]
	private static extern uint GetVersion(OsVersionInfo* lpVersionInformation);

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
	/// <param name="lpVersionInformation"><see cref="OsVersionInfo"/> that contains the version information about the currently running operating system.</param>
	/// <returns>True on success</returns>
	[DllImport(Kernel32, EntryPoint = "GetVersionExW", ExactSpelling = true, SetLastError = true)]
	private static extern int GetVersionFallback(OsVersionInfo* lpVersionInformation);

	/// <summary>
	/// Retrieves the product type for the operating system on the local computer, and maps the type to the product types supported by the specified operating system.
	/// <see href="https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getproductinfo"/>
	/// </summary>
	/// <param name="dwOsMajorVersion">The major version number of the operating system.</param>
	/// <param name="dwOsMinorVersion">The minor version number of the operating system.</param>
	/// <param name="dwSpMajorVersion">The major version number of the operating system service pack.</param>
	/// <param name="dwSpMinorVersion">The minor version number of the operating system service pack.</param>
	/// <param name="pdwReturnedProductType">The product type.</param>
	/// <returns>A nonzero value on success. This function fails if one of the input parameters is invalid.</returns>
	[DllImport(Kernel32, EntryPoint = "GetProductInfo", ExactSpelling = true)]
	private static extern int GetProductInfo(uint dwOsMajorVersion, uint dwOsMinorVersion, uint dwSpMajorVersion, uint dwSpMinorVersion, uint* pdwReturnedProductType);

	/// <summary>
	/// Returns the architecture info on the process and operating system.
	/// <see href="https://learn.microsoft.com/en-us/windows/win32/api/wow64apiset/nf-wow64apiset-iswow64process2"/>
	/// </summary>
	/// <param name="process">Target process</param>
	/// <param name="processArchitecture">Process architecture</param>
	/// <param name="systemArchitecture">System architecture</param>
	/// <returns>A nonzero value on success.</returns>
	[DllImport(Kernel32, EntryPoint = "IsWow64Process2", ExactSpelling = true)]
	private static extern int GetArchitecture(void* process, ushort* processArchitecture, ushort* systemArchitecture);

	/// <summary>
	/// Retrieves a pseudo handle for the current process.
	/// <see href="https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getcurrentprocess"/>
	/// </summary>
	/// <returns>A pseudo handle to the current process.</returns>
	[DllImport(Kernel32, EntryPoint = "GetCurrentProcess", ExactSpelling = true)]
	private static extern void* GetCurrentProcess();

	private static bool GetRegistryValue(string key, string value, uint sz, void* data, uint dataSize)
	{
		[DllImport(Advapi32, EntryPoint = "RegGetValueW", ExactSpelling = true)]
		static extern int RegGetValue(nint hkey, ushort* key, ushort* value, uint flags, uint* type, void* data, uint* dataLength);

		fixed (char* keyPointer = key)
		fixed (char* valuePointer = value)
			return RegGetValue(Hklm, (ushort*) keyPointer, (ushort*) valuePointer, sz, null, data, &dataSize) == 0;
	}

	/// <summary>
	/// Managed version of <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/ns-wdm-_osversioninfoexw"/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct OsVersionInfo
	{
		/// <summary>
		/// The marshalled size, in bytes, of an <see cref="OsVersionInfo"/> structure.
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
}
