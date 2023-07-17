using System.Collections.Generic;
using System.Linq;
using NorthwoodLib.Pools;
using Xunit;

namespace NorthwoodLib.Tests.Pools
{
	public class ListPoolTest
	{
		[Fact]
		public void ValidTest()
		{
			List<string> list = ListPool<string>.Shared.Rent();
			Assert.NotNull(list);
			Assert.Empty(list);
			ListPool<string>.Shared.Return(list);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(256)]
		[InlineData(512)]
		[InlineData(1024)]
		public void CapacityTest(int capacity)
		{
			List<string> list = ListPool<string>.Shared.Rent(capacity);
			Assert.True(list.Capacity >= capacity);
			ListPool<string>.Shared.Return(list);
		}

		[Theory]
		[InlineData(new object[] { new string[0] })]
		[InlineData(new object[] { new[] { "test" } })]
		[InlineData(new object[] { new[] { "test", "test2" } })]
		[InlineData(new object[] { new[] { "test", "test2", "test3" } })]
		public void SequenceTest(string[] input)
		{
			List<string> list = ListPool<string>.Shared.Rent(input);
			Assert.True(list.SequenceEqual(input));
			ListPool<string>.Shared.Return(list);
		}
	}
}
