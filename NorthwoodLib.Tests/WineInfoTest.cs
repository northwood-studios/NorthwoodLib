using NorthwoodLib.Tests.Utilities;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public class WineInfoTest : LoggingTest
	{
		public WineInfoTest(ITestOutputHelper output) : base(output)
		{
		}

		[Fact]
		public void NotWindowsTest()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return;
			Assert.False(WineInfo.UsesWine);
			Assert.Null(WineInfo.WineVersion);
		}

		[Fact]
		public void UsesWineTest()
		{
			Assert.Equal(WineInfo.UsesWine, WineInfo.WineVersion != null);
		}

		[Fact]
		public void WinePatchesTest()
		{
			if (WineInfo.WinePatches == null)
				return;
			Assert.False(string.IsNullOrWhiteSpace(WineInfo.WinePatches));
		}

		[Fact]
		public void WineHostTest()
		{
			if (WineInfo.WineHost == null)
				return;
			Assert.False(string.IsNullOrWhiteSpace(WineInfo.WineHost));
		}
	}
}
