using System;
using System.IO;
using NorthwoodLib.Logging;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests.Utilities
{
	internal sealed class XunitLogger : ITestOutputHelper, IDisposable
	{
		private readonly ITestOutputHelper _outputHelper;
		private readonly string _className;

		private static StreamWriter _writer;
		private static int _refCount;

		private static readonly string _logPath = Environment.GetEnvironmentVariable("xunitlogpath");
		private static readonly object _writeLock = new object();

		/// <summary>
		/// Logs data to XUnit output and log file set with environment variable xunitlogpath
		/// </summary>
		/// <param name="output">XUnit logger</param>
		/// <param name="type">Test class</param>
		public XunitLogger(ITestOutputHelper output, Type type)
		{
			_outputHelper = output;
			_className = type.FullName;

			lock (_writeLock)
			{
				if (_logPath == null)
					return;
				if (_refCount++ == 0)
					_writer = new StreamWriter(new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
			}
		}

		/// <summary>
		/// Writes a message to XUnit output and log file
		/// </summary>
		/// <param name="message">Text to write</param>
		public void WriteLine(string message)
		{
			_outputHelper?.WriteLine(message);
			lock (_writeLock)
				_writer?.WriteLine($"[{_className}] {message}\n{TruncateToLastNewline(Environment.StackTrace, 1000)}\n");
		}

		/// <summary>
		/// Writes a formatted message to XUnit output and log file
		/// </summary>
		/// <param name="format">Text used for formatting</param>
		/// <param name="args">Data inserted into format</param>
		public void WriteLine(string format, params object[] args)
		{
			_outputHelper?.WriteLine(format, args);
			lock (_writeLock)
				_writer?.WriteLine($"[{_className}] {string.Format(format, args)}\n{TruncateToLastNewline(Environment.StackTrace, 1000)}\n");
		}

		private static string TruncateToLastNewline(string text, int maxSize)
		{
			return text.Length <= maxSize ? text : text.Substring(0, text.LastIndexOf('\n', maxSize - 1, maxSize));
		}

		private static void ReleaseUnmanagedResources()
		{
			lock (_writeLock)
			{
				if (_writer == null)
					return;

				if (--_refCount == 0)
				{
					_writer.Dispose();
					_writer = null;
				}
			}
		}

		/// <summary>
		/// Releases the logfile stream
		/// </summary>
		public void Dispose()
		{
			ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}

		~XunitLogger()
		{
			ReleaseUnmanagedResources();
		}
	}
}
