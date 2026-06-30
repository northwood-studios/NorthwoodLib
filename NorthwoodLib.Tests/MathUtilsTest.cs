using Xunit;

namespace NorthwoodLib.Tests;

public class MathUtilsTest
{
	[Theory]
	[InlineData(0, 0)]
	[InlineData(uint.MaxValue, ushort.MaxValue)]
	[InlineData(uint.MaxValue ^ ushort.MaxValue, ushort.MaxValue)]
	public void HiwordTest(uint input, uint expected)
	{
		Assert.Equal(expected, MathUtils.Hiword(input));
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(uint.MaxValue, ushort.MaxValue)]
	[InlineData(ushort.MaxValue, ushort.MaxValue)]
	public void LowordTest(uint input, uint expected)
	{
		Assert.Equal(expected, MathUtils.Loword(input));
	}
}
