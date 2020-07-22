using System;
using System.Collections.Concurrent;
using System.Text;

namespace NorthwoodLib.Pools
{
	public static class StringBuilderPool
	{
		private static readonly ConcurrentQueue<StringBuilder> _pool = new ConcurrentQueue<StringBuilder>();

		public static StringBuilder Rent()
		{
			return _pool.TryDequeue(out StringBuilder stringBuilder) ? stringBuilder : new StringBuilder(512);
		}

		public static StringBuilder Rent(int capacity)
		{
			if (_pool.TryDequeue(out StringBuilder stringBuilder))
			{
				if (stringBuilder.Capacity < capacity)
					stringBuilder.Capacity = capacity;
				return stringBuilder;
			}

			return new StringBuilder(Math.Max(capacity, 512));
		}

		public static StringBuilder Rent(string text)
		{
			if (_pool.TryDequeue(out StringBuilder stringBuilder))
			{
				stringBuilder.Append(text);
				return stringBuilder;
			}

			return new StringBuilder(text, 512);
		}

		public static void Return(StringBuilder stringBuilder)
		{
			stringBuilder.Clear();
			_pool.Enqueue(stringBuilder);
		}
	}
}
