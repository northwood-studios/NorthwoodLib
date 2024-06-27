using System;
using System.Runtime.InteropServices;
using NorthwoodLib.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests;

public class WineInfoTest(ITestOutputHelper output) : LoggingTest(output)
{
	[Fact]
	public void NotWindowsTest()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return;
		Assert.False(WineInfo.UsesWine);
		Assert.Null(WineInfo.WineVersion);
		Assert.Null(WineInfo.WineHost);
	}

	[Fact]
	public void UsesWineTest()
	{
		if (WineInfo.WineVersion != null)
			Assert.True(WineInfo.UsesWine);
	}

	[Fact]
	public void UsesProtonTest()
	{
		if (WineInfo.UsesProton)
			Assert.True(WineInfo.UsesWine);
	}

	[Fact]
	[Obsolete("WinePatches is obsolete")]
	public void WinePatchesTest()
	{
		Assert.Null(WineInfo.WinePatches);
	}

	[Fact]
	public void WineHostTest()
	{
		if (WineInfo.WineHost == null)
			return;
		Assert.False(string.IsNullOrWhiteSpace(WineInfo.WineHost));
	}
}
