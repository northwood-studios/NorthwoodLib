using NorthwoodLib.Pools;
using System.Text;
using Xunit;

namespace NorthwoodLib.Tests.Pools
{
	public class StringBuilderPoolTest
	{
		[Fact]
		public void ValidTest()
		{
			StringBuilder sb = StringBuilderPool.Rent();
			Assert.NotNull(sb);
			Assert.True(sb.Length == 0);
			StringBuilderPool.Return(sb);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(256)]
		[InlineData(512)]
		[InlineData(1024)]
		public void CapacityTest(int capacity)
		{
			StringBuilder sb = StringBuilderPool.Rent(capacity);
			Assert.True(sb.Capacity >= capacity);
			StringBuilderPool.Return(sb);
		}

		[Theory]
		[InlineData("")]
		[InlineData("test 1")]
		[InlineData("test \n \0 \r")]
		public void TextTest(string input)
		{
			StringBuilder sb = StringBuilderPool.Rent(input);
			Assert.Equal(input, sb.ToString());
			StringBuilderPool.Return(sb);
		}
	}
}
