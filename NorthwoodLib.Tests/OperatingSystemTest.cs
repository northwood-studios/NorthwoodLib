using System;
using System.IO;
using System.Runtime.InteropServices;
using NorthwoodLib.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests;

public class OperatingSystemTest(ITestOutputHelper output) : LoggingTest(output)
{
	[Fact]
	public void UsesNativeDataTest()
	{
		Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || File.Exists("/etc/os-release"), OperatingSystem.UsesNativeData);
	}

	[Fact]
	[Obsolete("UsesWine is obsolete")]
	public void UsesWineTest()
	{
		Assert.Equal(WineInfo.UsesWine, OperatingSystem.UsesWine);
	}

	[Fact]
	public void ValidStringTest()
	{
		string version = OperatingSystem.VersionString;
		Logger.WriteLine(version);
		Assert.NotNull(version);
		Assert.NotEqual("", version);
	}

	[Fact]
	public void ValidVersionTest()
	{
		Version version = OperatingSystem.Version;
		Logger.WriteLine(version.ToString());
		Assert.NotEqual(new Version(0, 0, 0), version);
	}

	[Fact]
	public void TrueVersionTest()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return;

		Version version = OperatingSystem.Version;
		OperatingSystem.TryCheckWindowsFileVersion(out Version v, OperatingSystem.GetWindowsRegistryBuild());
		Logger.WriteLine(version.ToString());
		Logger.WriteLine(v.ToString());
		Assert.Equal(version.Major, v.Major);
		Assert.Equal(version.Minor, v.Minor);
		Assert.Equal(version.Build, v.Build);
	}

	[Fact]
	public unsafe void ArchitectureTest()
	{
		if (OperatingSystem.Version >= new Version(10, 0, 6299))
		{
			Assert.True(OperatingSystem.TryGetWindowsArchitecture(out Architecture processArchitecture, out Architecture systemArchitecture));
			Assert.Equal(RuntimeInformation.ProcessArchitecture, processArchitecture);
			Assert.Equal(RuntimeInformation.OSArchitecture, systemArchitecture);
		}
		Assert.Equal(RuntimeInformation.ProcessArchitecture, OperatingSystem.ProcessArchitecture);
		Assert.Equal(RuntimeInformation.OSArchitecture, OperatingSystem.SystemArchitecture);
		int expectedSize = OperatingSystem.ProcessArchitecture switch
		{
			Architecture.X86 => sizeof(uint),
			Architecture.Arm => sizeof(uint),
			Architecture.Wasm => sizeof(uint),
			Architecture.S390x => sizeof(uint),
			Architecture.Armv6 => sizeof(uint),
			Architecture.X64 => sizeof(ulong),
			Architecture.Arm64 => sizeof(ulong),
			Architecture.LoongArch64 => sizeof(ulong),
			Architecture.Ppc64le => sizeof(ulong),
			Architecture.RiscV64 => sizeof(ulong),
			_ => sizeof(nuint)
		};
		Assert.Equal(sizeof(nuint), expectedSize);
	}
}
