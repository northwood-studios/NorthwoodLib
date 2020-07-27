using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NorthwoodLib.Pools
{
	public sealed class ListPool<T> : IPool<List<T>>
	{
		public static readonly ListPool<T> Shared = new ListPool<T>();

		private readonly ConcurrentQueue<List<T>> _pool = new ConcurrentQueue<List<T>>();

		public List<T> Rent()
		{
			return _pool.TryDequeue(out List<T> list) ? list : new List<T>(512);
		}

		public List<T> Rent(int capacity)
		{
			if (_pool.TryDequeue(out List<T> list))
			{
				if (list.Capacity < capacity)
					list.Capacity = capacity;
				return list;
			}

			return new List<T>(Math.Max(capacity, 512));
		}

		public List<T> Rent(IEnumerable<T> enumerable)
		{
			if (_pool.TryDequeue(out List<T> list))
			{
				list.AddRange(enumerable);
				return list;
			}

			return new List<T>(enumerable);
		}

		public void Return(List<T> list)
		{
			list.Clear();
			_pool.Enqueue(list);
		}
	}
}
