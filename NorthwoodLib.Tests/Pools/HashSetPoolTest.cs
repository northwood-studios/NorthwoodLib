using System.Collections.Generic;
using System.Linq;
using NorthwoodLib.Pools;
using Xunit;

namespace NorthwoodLib.Tests.Pools;

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
#if NETCOREAPP
		Assert.True(set.EnsureCapacity(0) >= capacity);
#endif
		HashSetPool<string>.Shared.Return(set);
	}

	[Theory]
	[InlineData([new string[0]])]
	[InlineData([new[] { "test" }])]
	[InlineData([new[] { "test", "test2" }])]
	[InlineData([new[] { "test", "test2", "test3" }])]
	public void SequenceTest(string[] input)
	{
		HashSet<string> set = HashSetPool<string>.Shared.Rent(input);
		Assert.True(set.SequenceEqual(input));
		HashSetPool<string>.Shared.Return(set);
	}
}
