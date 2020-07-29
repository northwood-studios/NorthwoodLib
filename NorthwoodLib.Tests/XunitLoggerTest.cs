using NorthwoodLib.Tests.Utilities;
using System;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public sealed class XunitLoggerTest : IDisposable
	{
		private readonly XunitLogger _logger;
		public XunitLoggerTest(ITestOutputHelper output) => _logger = new XunitLogger(output, GetType());

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

		private void Close()
		{
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
