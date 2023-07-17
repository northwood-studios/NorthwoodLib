using System;
using NorthwoodLib.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace NorthwoodLib.Tests
{
	public class StringUtilsTest : LoggingTest
	{
		public StringUtilsTest(ITestOutputHelper output) : base(output)
		{
		}

		[Theory]
		[InlineData(5, '8', "1234567890", "12345")]
		[InlineData(5, '3', "1234567890", "123")]
		[InlineData(5, '2', "1232567890", "1232")]
		public void TruncateToLastCharTest(int maxSize, char c, string text, string truncated)
		{
			string tr = text.TruncateToLast(maxSize, c);
			Logger.WriteLine(tr);
			Assert.True(tr.Length <= maxSize);
			Assert.Equal(truncated, tr);
		}

		[Theory]
		[InlineData(5, "8", StringComparison.Ordinal, "1234567890", "12345")]
		[InlineData(5, "3", StringComparison.Ordinal, "1234567890", "123")]
		[InlineData(5, "2", StringComparison.Ordinal, "1232567890", "1232")]
		[InlineData(5, "a", StringComparison.Ordinal, "bcad54gfd", "bca")]
		[InlineData(5, "a", StringComparison.OrdinalIgnoreCase, "BCAD54GFD", "BCA")]
		[InlineData(5, "ad", StringComparison.Ordinal, "bcad54gfd", "bcad")]
		[InlineData(5, "ad", StringComparison.OrdinalIgnoreCase, "BCAD54GFD", "BCAD")]
		[InlineData(5, "A", StringComparison.Ordinal, "bcad54gfd", "bcad5")]
		public void TruncateToLastStringTest(int maxSize, string s, StringComparison comparision, string text, string truncated)
		{
			string tr = text.TruncateToLast(maxSize, s, comparision);
			Logger.WriteLine(tr);
			Assert.True(tr.Length <= maxSize);
			Assert.Equal(truncated, tr);
		}

		[Theory]
		[InlineData("")]
		[InlineData("fdds DASFD żąćźż \n \0 test")]
		public void Base64Test(string text)
		{
			string base64 = StringUtils.Base64Encode(text);
			Logger.WriteLine($"{text} - {base64}");
			Assert.Equal(text, StringUtils.Base64Decode(base64));
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("fdds DASFD żąćźż \n \0 test", "ZmRkcyBEQVNGRCDFvMSFxIfFusW8IAogACB0ZXN0")]
		public void Base64EncodeDecodeTest(string text, string base64)
		{
			Assert.Equal(text, StringUtils.Base64Decode(base64));
			Assert.Equal(base64, StringUtils.Base64Encode(text));
		}

		[Theory]
		[InlineData("1234567890", '6', true)]
		[InlineData("1234567890", 'c', false)]
		public void ContainsCharTest(string text, char c, bool contains)
		{
			Assert.Equal(contains, text.Contains(c));
		}

		[Theory]
		[InlineData("1234567890", "6", StringComparison.Ordinal, true)]
		[InlineData("1234567890", "c", StringComparison.Ordinal, false)]
		[InlineData("abcd", "c", StringComparison.Ordinal, true)]
		[InlineData("abcd", "C", StringComparison.Ordinal, false)]
		[InlineData("abcd", "C", StringComparison.OrdinalIgnoreCase, true)]
		[InlineData("abcd", "bc", StringComparison.Ordinal, true)]
		[InlineData("abcd", "BC", StringComparison.Ordinal, false)]
		[InlineData("abcd", "BC", StringComparison.OrdinalIgnoreCase, true)]
		public void ContainsStringTest(string text, string s, StringComparison comparision, bool contains)
		{
			Assert.Equal(contains, text.Contains(s, comparision));
		}

		[Theory]
		[InlineData("")]
		[InlineData("abcd")]
		[InlineData("abcd \0 \u0000 \u007F \n żźććą 主江热")]
		public void StripUnicodeCharactersTest(string text)
		{
			Assert.DoesNotMatch(@"[^\u0000-\u007F]", StringUtils.StripUnicodeCharacters(text));
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("abcd", "abcd")]
		[InlineData("ab cd \0 \u0000 \u007F \n żźććą 主江热", "ab cd      ")]
		public void RemoveSpecialCharactersTest(string text, string output)
		{
			Assert.Equal(output, StringUtils.RemoveSpecialCharacters(text));
		}

		[Theory]
		[InlineData("", "color")]
		[InlineData("abcd", "color")]
		[InlineData("ab cd \0 \u0000 \u007F \n żźććą 主江热", "color")]
		[InlineData("ab cd \0 \u0000 <color=red>text</color> \u007F \n żźććą 主江热", "color")]
		[InlineData("ab cd \0 \u0000 <color=red>text</color> <size=5>b</size> <br/> \u007F \n żźććą 主江热", "color")]
		[InlineData("ab cd \0 \u0000 <color=red>text</color> <size=5>b</size> <br/> \u007F \n żźććą 主江热", "br")]
		public void StripTagTest(string text, string tag)
		{
			Assert.DoesNotMatch($"<.*?{tag}.*?>", StringUtils.StripTag(text, tag));
		}

		[Theory]
		[InlineData("")]
		[InlineData("abcd")]
		[InlineData("ab cd \0 \u0000 \u007F \n żźććą 主江热")]
		[InlineData("ab cd \0 \u0000 <color=red>text</color> \u007F \n żźććą 主江热")]
		[InlineData("ab cd \0 \u0000 <color=red>text</color> <size=5>b</size> <br/> \u007F \n żźććą 主江热")]
		public void StripTagsTest(string text)
		{
			Assert.DoesNotMatch("<.*?>", StringUtils.StripTags(text));
		}
	}
}
