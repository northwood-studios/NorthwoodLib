using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NorthwoodLib.Pools
{
	public static class ListPool<T>
	{
		private static readonly ConcurrentQueue<List<T>> _pool = new ConcurrentQueue<List<T>>();

		public static List<T> Rent()
		{
			return _pool.TryDequeue(out List<T> list) ? list : new List<T>(512);
		}

		public static List<T> Rent(int capacity)
		{
			if (_pool.TryDequeue(out List<T> list))
			{
				if (list.Capacity < capacity)
					list.Capacity = capacity;
				return list;
			}

			return new List<T>(Math.Max(capacity, 512));
		}

		public static List<T> Rent(IEnumerable<T> enumerable)
		{
			if (_pool.TryDequeue(out List<T> list))
			{
				list.AddRange(enumerable);
				return list;
			}

			return new List<T>(enumerable);
		}

		public static void Return(List<T> list)
		{
			list.Clear();
			_pool.Enqueue(list);
		}
	}
}
