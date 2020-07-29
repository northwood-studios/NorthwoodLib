using System;
using System.IO;
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

		public void WriteLine(string message)
		{
			_outputHelper?.WriteLine(message);
			lock (_writeLock)
				_writer?.WriteLine($"[{_className}] {message}\n{TruncateToLastNewline(Environment.StackTrace, 2000)}\n");
		}

		public void WriteLine(string format, params object[] args)
		{
			_outputHelper?.WriteLine(format, args);
			lock (_writeLock)
				_writer?.WriteLine($"[{_className}] {string.Format(format, args)}\n{TruncateToLastNewline(Environment.StackTrace, 2000)}\n");
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
