using System;
using NorthwoodLib.Logging;

namespace NorthwoodLib
{
	/// <summary>
	/// Stores static data provided by the software using the library
	/// </summary>
	public static class PlatformSettings
	{
		/// <summary>
		/// Logs all messages from the code
		/// </summary>
		public static event Action<string, LogType> Logged;

		internal static void Log(string message, LogType type) => Logged?.Invoke(message, type);
	}
}
