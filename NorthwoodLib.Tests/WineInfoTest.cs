using NorthwoodLib.Logging;
using NorthwoodLib.Tests.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public class WineInfoTest : IDisposable
	{
		private readonly XunitLogger _logger;
		private readonly int _currentThread;
		public WineInfoTest(ITestOutputHelper output)
		{
			_logger = new XunitLogger(output, GetType());
			_currentThread = Thread.CurrentThread.ManagedThreadId;
			PlatformSettings.Logged += Log;
		}

		private void Log(string message, LogType type)
		{
			if (Thread.CurrentThread.ManagedThreadId == _currentThread)
				_logger.WriteLine(message);
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

		private void Close()
		{
			PlatformSettings.Logged -= Log;
			_logger.Dispose();
		}

		public void Dispose()
		{
			Close();
			GC.SuppressFinalize(this);
		}

		~WineInfoTest()
		{
			Close();
		}
	}
}
