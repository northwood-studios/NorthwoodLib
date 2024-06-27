using System;
using NorthwoodLib.Logging;

namespace NorthwoodLib;

/// <summary>
/// Stores static data provided by the software using the library
/// </summary>
public static class PlatformSettings
{
	/// <summary>
	/// Current library version
	/// </summary>
	internal const string VersionConst = "1.3.1";

	/// <summary>
	/// Returns the library version
	/// </summary>
	public static string Version => VersionConst;

	/// <summary>
	/// Logs all messages from the code
	/// </summary>
	public static event Action<string, LogType> Logged;

	/// <summary>
	/// Fires <see cref="Logged"/> with provided data
	/// </summary>
	/// <param name="message">Message text</param>
	/// <param name="type">Message type</param>
	internal static void Log(string message, LogType type) => Logged?.Invoke(message, type);
}
