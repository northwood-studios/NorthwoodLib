using System;
using System.Runtime.InteropServices;

namespace NorthwoodLib
{
	/// <summary>
	/// Provides data about currently used Operating System
	/// </summary>
	public static partial class OperatingSystem
	{
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
			UsesNativeData = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
				TryGetWindowsVersion(out Version version, out string os) :
				TryGetUnixOs(out version, out os);
			Version = UsesNativeData ? version : Environment.OSVersion.Version;
			VersionString = UsesNativeData ? os : $"{Environment.OSVersion.VersionString} {Environment.OSVersion.ServicePack}".Trim();
#pragma warning disable 618
			UsesWine = WineInfo.UsesWine;
#pragma warning restore 618
		}
	}
}
