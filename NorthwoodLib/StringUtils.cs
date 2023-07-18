using System;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using NorthwoodLib.Pools;

namespace NorthwoodLib
{
	/// <summary>
	/// Utility methods for <see cref="string"/>
	/// </summary>
	public static class StringUtils
	{
		private static readonly Regex UnicodeRegex = new(@"[^\u0000-\u007F]", RegexOptions.Compiled);
		private static readonly Regex TagRegex = new("<.*?>", RegexOptions.Compiled);

		/// <summary>
		/// Truncates a string to the last occurance of <see paramref="character"/> within <see paramref="maxSize"/> characters if it's longer than it
		/// </summary>
		/// <param name="text">Processed text</param>
		/// <param name="maxSize">Maximum size</param>
		/// <param name="character">Checked character</param>
		/// <returns>Truncated <see paramref="text"/></returns>
		public static string TruncateToLast(this string text, int maxSize, char character)
		{
			int index = text.LastIndexOf(character, maxSize - 1, maxSize);
			return text.Length <= maxSize ? text : text[..(index == -1 ? maxSize : index + 1)];
		}

		/// <summary>
		/// Truncates a string to the last occurance of <see paramref="str"/> within <see paramref="maxSize"/> characters if it's longer than it
		/// </summary>
		/// <param name="text">Processed text</param>
		/// <param name="maxSize">Maximum size</param>
		/// <param name="str">Checked string</param>
		/// <param name="comparison">String comparison</param>
		/// <returns>Truncated <see paramref="text"/></returns>
		public static string TruncateToLast(this string text, int maxSize, string str, StringComparison comparison = StringComparison.Ordinal)
		{
			int index = text.LastIndexOf(str, maxSize - 1, maxSize, comparison);
			return text.Length <= maxSize ? text : text[..(index == -1 ? maxSize : index + str.Length)];
		}

		/// <summary>
		/// Converts a text to Base64 encoded UTF8
		/// </summary>
		/// <param name="plainText">Processed text</param>
		/// <returns>Base64 encoded UTF8 of <see paramref="plainText"/></returns>
		public static string Base64Encode(string plainText)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(plainText.Length));
			int plainTextByteCount = Encoding.UTF8.GetBytes(plainText, 0, plainText.Length, buffer, 0);
			string result = Convert.ToBase64String(buffer, 0, plainTextByteCount);
			ArrayPool<byte>.Shared.Return(buffer);
			return result;
		}

		/// <summary>
		/// Converts Base64 encoded UTF8 data to text
		/// </summary>
		/// <param name="base64EncodedData">Base64 encoded UTF8 data</param>
		/// <returns>Converted text</returns>
		public static string Base64Decode(string base64EncodedData)
		{
			byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
			return Encoding.UTF8.GetString(base64EncodedBytes);
		}

		/// <summary>
		/// Returns a value indicating whether a specified character occurs within this string.
		/// </summary>
		/// <param name="s">Checked string</param>
		/// <param name="value">The character to seek.</param>
		/// <returns><see langword="true"/> if the value parameter occurs within this string; otherwise, <see langword="false"/>.</returns>
		public static bool Contains(this string s, char value)
		{
			return s.IndexOf(value) >= 0;
		}

		/// <summary>
		/// Returns a value indicating whether a specified string occurs within this string, using the specified comparison rules.
		/// </summary>
		/// <param name="s">Checked string</param>
		/// <param name="value">The string to seek.</param>
		/// <param name="comparison">One of the enumeration values that specifies the rules to use in the comparison.</param>
		/// <returns><see langword="true"/> if the <see paramref="value"/> occurs within this string, or if <see paramref="value"/> is the empty string (""); otherwise, <see langword="false"/>.</returns>
		public static bool Contains(this string s, string value, StringComparison comparison)
		{
			return s.IndexOf(value, comparison) >= 0;
		}

		/// <summary>
		/// Replaces Unicode characters in a string.
		/// </summary>
		/// <param name="input">Input string</param>
		/// <param name="replacement">String to replace Unicode characters with</param>
		/// <returns>Processed string</returns>
		public static string StripUnicodeCharacters(string input, string replacement = "")
		{
			return UnicodeRegex.Replace(input, replacement);
		}

		/// <summary>
		/// Removes special characters from provided text
		/// </summary>
		/// <param name="str">Processed text</param>
		/// <returns>Filtered text</returns>
		public static string RemoveSpecialCharacters(string str)
		{
			StringBuilder sb = StringBuilderPool.Shared.Rent(str.Length);
			foreach (char c in str)
				if (c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or ' ' or '-' or '.' or ',' or '_')
					sb.Append(c);
			string text = sb.Length == str.Length ? str : sb.ToString();
			StringBuilderPool.Shared.Return(sb);

			return text;
		}

		/// <summary>
		/// Removes a tag from a string
		/// </summary>
		/// <param name="input">Processed text</param>
		/// <param name="tag">Removed tag</param>
		/// <returns>Filtered text</returns>
		public static string StripTag(string input, string tag)
		{
			return Regex.Replace(input, $"<.*?{tag}.*?>", string.Empty);
		}

		/// <summary>
		/// Removes tags from a string
		/// </summary>
		/// <param name="input">Processed text</param>
		/// <returns>Filtered text</returns>
		public static string StripTags(string input)
		{
			return TagRegex.Replace(input, string.Empty);
		}
	}
}
