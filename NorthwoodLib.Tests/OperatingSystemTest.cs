using NorthwoodLib.Logging;
using NorthwoodLib.Tests.Utilities;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public class OperatingSystemTest : IDisposable
	{
		private readonly XunitLogger _logger;
		private readonly int _currentThread;
		public OperatingSystemTest(ITestOutputHelper output)
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
			_logger.WriteLine(version);
			Assert.NotNull(version);
			Assert.NotEqual("", version);
		}

		[Fact]
		public void CorrectVersionTest()
		{
			Version version = OperatingSystem.Version;
			_logger.WriteLine(version.ToString());
			Assert.NotEqual(new Version(0, 0, 0), version);
		}

		[Fact]
		public void TrueVersionTest()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return;

			Version version = OperatingSystem.Version;
			OperatingSystem.OSVERSIONINFO info = new OperatingSystem.OSVERSIONINFO
			{ dwMajorVersion = 6, dwMinorVersion = 2, dwBuildNumber = 9200 };
			OperatingSystem.CheckTrueVersion(ref info);
			Assert.Equal(version.Major, (int) info.dwMajorVersion);
			Assert.Equal(version.Minor, (int) info.dwMinorVersion);
			Assert.Equal(version.Build, (int) info.dwBuildNumber);
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

		~OperatingSystemTest()
		{
			Close();
		}
	}
}
