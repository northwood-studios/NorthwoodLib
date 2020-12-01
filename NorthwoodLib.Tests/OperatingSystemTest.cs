using NorthwoodLib.Tests.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public class OperatingSystemTest : LoggingTest
	{
		public OperatingSystemTest(ITestOutputHelper output) : base(output)
		{
		}

		[Fact]
		public void UsesNativeDataTest()
		{
			Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || File.Exists("/etc/os-release"), OperatingSystem.UsesNativeData);
		}

		[Fact]
		public void UsesWineTest()
		{
#pragma warning disable 618
			Assert.Equal(WineInfo.UsesWine, OperatingSystem.UsesWine);
#pragma warning restore 618
		}

		[Fact]
		public void CorrectStringTest()
		{
			string version = OperatingSystem.VersionString;
			Logger.WriteLine(version);
			Assert.NotNull(version);
			Assert.NotEqual("", version);
		}

		[Fact]
		public void CorrectVersionTest()
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
			OperatingSystem.TryCheckWindowsFileVersion(out Version v);
			Assert.Equal(version.Major, v.Major);
			Assert.Equal(version.Minor, v.Minor);
			Assert.Equal(version.Build, v.Build);
		}
	}
}
