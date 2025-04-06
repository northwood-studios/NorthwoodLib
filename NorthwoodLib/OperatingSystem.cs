using System;
using System.Runtime.InteropServices;

namespace NorthwoodLib;

/// <summary>
/// Provides data about the currently used Operating System.
/// </summary>
public static partial class OperatingSystem
{
	/// <summary>
	/// Informs if code uses P/Invokes to obtain the data.
	/// </summary>
	public static readonly bool UsesNativeData;

	/// <summary>
	/// Informs if the user uses Wine. User can hide Wine so don't rely on this for anything other than diagnostic usage.
	/// </summary>
	[Obsolete("Use WineInfo.UsesWine instead")]
	public static readonly bool UsesWine;

	/// <summary>
	/// Used Operating System <see cref="System.Version"/>.
	/// </summary>
	public static readonly Version Version;

	/// <summary>
	/// Returns a human readable description of the used Operating System.
	/// </summary>
	public static readonly string VersionString;

	/// <summary>
	/// <see cref="Architecture"/> of the current process.
	/// </summary>
	public static readonly Architecture ProcessArchitecture;

	/// <summary>
	/// <see cref="Architecture"/> of the operating system.
	/// </summary>
	public static readonly Architecture SystemArchitecture;

	static OperatingSystem()
	{
		UsesNativeData = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
			TryGetWindowsVersion(out Version version, out string os) :
			TryGetUnixOs(out version, out os);

		Version = UsesNativeData ? version : Environment.OSVersion.Version;
		VersionString = UsesNativeData ? os : $"{Environment.OSVersion.VersionString} {Environment.OSVersion.ServicePack}".Trim();

		if (!TryGetWindowsArchitecture(out ProcessArchitecture, out SystemArchitecture))
		{
			ProcessArchitecture = RuntimeInformation.ProcessArchitecture;
			SystemArchitecture = RuntimeInformation.OSArchitecture;
		}

		VersionString = ProcessArchitecture == SystemArchitecture ?
			$"{VersionString} {ProcessArchitecture}" :
			$"{VersionString} {ProcessArchitecture}-on-{SystemArchitecture}";

#pragma warning disable 618
		UsesWine = WineInfo.UsesWine;
#pragma warning restore 618
	}
}
