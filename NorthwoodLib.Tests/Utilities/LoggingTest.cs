using NorthwoodLib.Logging;
using System;
using System.Threading;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests.Utilities
{
	/// <summary>
	/// Base for tests using <see cref="XunitLogger"/>
	/// </summary>
	public abstract class LoggingTest : IDisposable
	{
		protected readonly XunitLogger Logger;
		private readonly int _currentThread;

		/// <summary>
		/// Creates the <see cref="Logger"/> and starts listening to <see cref="PlatformSettings.Logged"/> with it
		/// </summary>
		/// <param name="output">Xunit output handler</param>
		protected LoggingTest(ITestOutputHelper output)
		{
			Logger = new XunitLogger(output, GetType());
			_currentThread = Thread.CurrentThread.ManagedThreadId;
			PlatformSettings.Logged += Log;
		}

		private void Log(string message, LogType type)
		{
			if (Thread.CurrentThread.ManagedThreadId == _currentThread)
				Logger.WriteLine(message);
		}

		private void Close()
		{
			PlatformSettings.Logged -= Log;
			Logger.Dispose();
		}

		/// <summary>
		/// Disposes the <see cref="Logger"/>
		/// </summary>
		public void Dispose()
		{
			Close();
			GC.SuppressFinalize(this);
		}

		~LoggingTest()
		{
			Close();
		}
	}
}
