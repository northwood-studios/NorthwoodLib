using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public class OperatingSystemTest
	{
		private readonly ITestOutputHelper _output;
		public OperatingSystemTest(ITestOutputHelper output) => _output = output;

		[Fact]
		public void UsesNativeDataTest()
		{
			Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), OperatingSystem.UsesNativeData);
		}

		[Fact]
		public void CorrectStringTest()
		{
			string version = OperatingSystem.VersionString;
			_output.WriteLine(version);
			Assert.NotNull(version);
			Assert.NotEqual("", version);
		}

		[Fact]
		public void CorrectVersionTest()
		{
			Version version = OperatingSystem.Version;
			_output.WriteLine(version.ToString());
			Assert.NotEqual(new Version(0, 0, 0), version);
		}
	}
}
