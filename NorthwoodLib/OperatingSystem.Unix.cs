using System;
using System.IO;
using NorthwoodLib.Logging;

namespace NorthwoodLib;

public static partial class OperatingSystem
{
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
			using (FileStream fs = new(osrelease, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (StreamReader sr = new(fs))
				while (sr.ReadLine() is { } line)
				{
					ReadOnlySpan<char> value = line.AsSpan().Trim();

					const string prettyname = "PRETTY_NAME=";
					if (!value.StartsWith(prettyname))
						continue;

					value = value[prettyname.Length..].Trim();

					name = $"{value.Trim('"').Trim().ToString()} {version}".Trim();
					return true;
				}
		}
		catch (FileNotFoundException)
		{
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (Exception ex)
		{
			PlatformSettings.Log($"Error while reading {osrelease}: {ex.Message}", LogType.Warning);
		}

		name = null;
		return false;
	}
}
