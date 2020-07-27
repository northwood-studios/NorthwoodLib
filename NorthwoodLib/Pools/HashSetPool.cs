using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NorthwoodLib.Pools
{
	public sealed class HashSetPool<T> : IPool<HashSet<T>>
	{
		public static readonly HashSetPool<T> Shared = new HashSetPool<T>();

		private readonly ConcurrentQueue<HashSet<T>> _pool = new ConcurrentQueue<HashSet<T>>();

		public HashSet<T> Rent()
		{
			return _pool.TryDequeue(out HashSet<T> set) ? set : new HashSet<T>(512);
		}

		public HashSet<T> Rent(int capacity)
		{
			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (_pool.TryDequeue(out HashSet<T> set))
			{
#if NETSTANDARD
				set.EnsureCapacity(capacity);
#endif
				return set;
			}

			return new HashSet<T>(Math.Max(capacity, 512));
		}

		public HashSet<T> Rent(IEnumerable<T> enumerable)
		{
			if (_pool.TryDequeue(out HashSet<T> set))
			{
				if (enumerable is IReadOnlyList<T> list)
					for (int i = 0; i < list.Count; i++)
						set.Add(list[i]);
				else
					foreach (T t in enumerable)
						set.Add(t);

				return set;
			}

			return new HashSet<T>(enumerable);
		}

		public void Return(HashSet<T> set)
		{
			set.Clear();
			_pool.Enqueue(set);
		}
	}
}
