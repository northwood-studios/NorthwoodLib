using System;
using System.IO;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests.Utilities;

/// <summary>
/// Logs data to XUnit output and log file set with environment variable xunitlogpath
/// </summary>
public sealed class XunitLogger : ITestOutputHelper, IDisposable
{
	private readonly ITestOutputHelper _outputHelper;
	private readonly string _className;

	private static StreamWriter _writer;
	private static int _refCount;

	private static readonly string LogPath = Environment.GetEnvironmentVariable("xunitlogpath");
	private static readonly object WriteLock = new();

	/// <summary>
	/// Logs data to XUnit output and log file set with environment variable xunitlogpath
	/// </summary>
	/// <param name="output">XUnit logger</param>
	/// <param name="type">Test class</param>
	public XunitLogger(ITestOutputHelper output, Type type)
	{
		_outputHelper = output;
		_className = type.FullName;

		lock (WriteLock)
		{
			if (LogPath == null)
				return;
			if (_refCount++ == 0)
				_writer = new StreamWriter(new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
		}
	}

	/// <summary>
	/// Writes a message to XUnit output and log file
	/// </summary>
	/// <param name="message">Text to write</param>
	public void WriteLine(string message)
	{
		_outputHelper?.WriteLine(message);
		lock (WriteLock)
			_writer?.WriteLine($"[{_className}] {message}\n{Environment.StackTrace.TruncateToLast(1000, '\n').TrimEnd()}\n");
	}

	/// <summary>
	/// Writes a formatted message to XUnit output and log file
	/// </summary>
	/// <param name="format">Text used for formatting</param>
	/// <param name="args">Data inserted into format</param>
	public void WriteLine(string format, params object[] args)
	{
		_outputHelper?.WriteLine(format, args);
		lock (WriteLock)
			_writer?.WriteLine($"[{_className}] {string.Format(format, args)}\n{Environment.StackTrace.TruncateToLast(1000, '\n').TrimEnd()}\n");
	}

	private static void ReleaseUnmanagedResources()
	{
		lock (WriteLock)
		{
			if (_writer == null)
				return;

			if (--_refCount != 0)
				return;

			_writer.Dispose();
			_writer = null;
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
