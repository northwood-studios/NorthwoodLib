using System;
using Xunit;

namespace NorthwoodLib.Tests
{
	public class OperatingSystemTest
	{
		[Fact]
		public void CorrectStringTest()
		{
			Assert.Equal(OperatingSystem.UseNativeData ? OperatingSystem.NativeVersionString : Environment.OSVersion.VersionString, OperatingSystem.GetSystemVersionString());
		}

		[Fact]
		public void CorrectVersionTest()
		{
			Assert.Equal(OperatingSystem.UseNativeData ? OperatingSystem.NativeVersion : Environment.OSVersion.Version, OperatingSystem.GetSystemVersion());
		}
	}
}
