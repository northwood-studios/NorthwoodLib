using System;
using Xunit;

namespace NorthwoodLib.Tests
{
	public class NativeMemoryTest
	{
		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(16384)]
		public void CreateTest(int size)
		{
			using (NativeMemory memory = new(size))
			{
				Assert.NotEqual(IntPtr.Zero, memory.Data);
				Assert.Equal(size, memory.Length);
			}
		}

		[Fact]
		public unsafe void AccessTest()
		{
			using (NativeMemory memory = new(sizeof(int) * 10))
			{
				int* ptr = memory.ToPointer<int>();
				for (int i = 0; i < 10; i++)
					ptr[i] = i;

				for (int i = 0; i < 10; i++)
					Assert.Equal(i, ptr[i]);
			}
		}

		[Fact]
		public unsafe void PointerTest()
		{
			using (NativeMemory memory = new(sizeof(int) * 10))
				Assert.Equal(memory.Data, new IntPtr(memory.ToPointer<int>()));
		}
	}
}
