using System;
using NorthwoodLib.Logging;

namespace NorthwoodLib.Tester;

public static class Program
{
	public static void Main()
	{
		PlatformSettings.Logged += Log;

		Console.WriteLine($"Loaded {typeof(PlatformSettings).Assembly.GetName().Name} version {PlatformSettings.Version}");
		Console.WriteLine();
		Console.WriteLine($"Uses native data: {OperatingSystem.UsesNativeData}");
		Console.WriteLine($"OS version: {OperatingSystem.Version}");
		Console.WriteLine($"OS description: {OperatingSystem.VersionString}");
		Console.WriteLine($"Process architecture: {OperatingSystem.ProcessArchitecture}");
		Console.WriteLine($"System architecture: {OperatingSystem.SystemArchitecture}");
		Console.WriteLine();
		Console.WriteLine($"Uses Wine: {WineInfo.UsesWine}");
		Console.WriteLine($"Uses Proton: {WineInfo.UsesProton}");
		Console.WriteLine($"Wine version: {WineInfo.WineVersion}");
		Console.WriteLine($"Wine host: {WineInfo.WineHost}");
	}

	private static void Log(string message, LogType type)
	{
		Console.WriteLine($"[{type.ToString().ToUpperInvariant()}] {message}");
	}
}
