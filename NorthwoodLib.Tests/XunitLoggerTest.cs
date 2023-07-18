using System;
using System.Threading;
using NorthwoodLib.Logging;
using NorthwoodLib.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public sealed class XunitLoggerTest : IDisposable
	{
		private bool _logged;

		private readonly XunitLogger _logger;
		private readonly int _currentThread;
		public XunitLoggerTest(ITestOutputHelper output)
		{
			_logger = new XunitLogger(output, GetType());
			_currentThread = Thread.CurrentThread.ManagedThreadId;
			PlatformSettings.Logged += Log;
		}

		private void Log(string message, LogType type)
		{
			// check thread id cause unit tests run in parallel
			if (Thread.CurrentThread.ManagedThreadId == _currentThread)
			{
				_logger.WriteLine(message);
				_logged = true;
			}
		}

		[Theory]
		[InlineData("XunitLoggerTest")]
		[InlineData("Test data \n data żźćńąśłę€ó")]
		public void WriteTest(string text)
		{
			_logger.WriteLine(text);
		}

		[Theory]
		[InlineData("{0} {1} {2}", new object[] { 1, 2, 3 })]
		public void WriteFormatTest(string format, object[] args)
		{
			_logger.WriteLine(format, args);
		}

		[Theory]
		[InlineData("XunitLoggerTest")]
		[InlineData("Test data \n data żźćńąśłę€ó")]
		public void EventTest(string text)
		{
			PlatformSettings.Log(text, LogType.Info);
			Assert.True(_logged);
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

		~XunitLoggerTest()
		{
			Close();
		}
	}
}
