using NorthwoodLib.Pools;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NorthwoodLib.Tests.Pools
{
	public class HashSetPoolTest
	{
		[Fact]
		public void ValidTest()
		{
			HashSet<string> set = HashSetPool<string>.Shared.Rent();
			Assert.NotNull(set);
			Assert.Empty(set);
			HashSetPool<string>.Shared.Return(set);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(256)]
		[InlineData(512)]
		[InlineData(1024)]
		public void CapacityTest(int capacity)
		{
			HashSet<string> set = HashSetPool<string>.Shared.Rent(capacity);
			Assert.True(set.EnsureCapacity(0) >= capacity);
			HashSetPool<string>.Shared.Return(set);
		}

		[Theory]
		[InlineData(new object[] { new string[0] })]
		[InlineData(new object[] { new[] { "test" } })]
		[InlineData(new object[] { new[] { "test", "test2" } })]
		[InlineData(new object[] { new[] { "test", "test2", "test3" } })]
		public void SequenceTest(string[] input)
		{
			HashSet<string> set = HashSetPool<string>.Shared.Rent(input);
			Assert.True(set.SequenceEqual(input));
			HashSetPool<string>.Shared.Return(set);
		}
	}
}
