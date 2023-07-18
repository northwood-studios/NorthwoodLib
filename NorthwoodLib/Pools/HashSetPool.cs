using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NorthwoodLib.Pools
{
	/// <summary>
	/// Returns pooled <see cref="HashSet{T}"/>
	/// </summary>
	/// <typeparam name="T">Element type</typeparam>
	public sealed class HashSetPool<T> : IPool<HashSet<T>>
	{
		/// <summary>
		/// Gets a shared <see cref="HashSetPool{T}"/> instance
		/// </summary>
		public static readonly HashSetPool<T> Shared = new();

		private readonly ConcurrentQueue<HashSet<T>> _pool = new();

		/// <summary>
		/// Gives a pooled <see cref="HashSet{T}"/>
		/// </summary>
		/// <returns><see cref="HashSet{T}"/> from the pool</returns>
		public HashSet<T> Rent()
		{
			return _pool.TryDequeue(out HashSet<T> set) ? set : new HashSet<T>(512);
		}

		/// <summary>
		/// Gives a pooled <see cref="HashSet{T}"/> with provided capacity
		/// </summary>
		/// <param name="capacity">Requested capacity</param>
		/// <returns><see cref="HashSet{T}"/> from the pool</returns>
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

		/// <summary>
		/// Gives a pooled <see cref="HashSet{T}"/> with initial content
		/// </summary>
		/// <param name="enumerable">Initial content</param>
		/// <returns><see cref="HashSet{T}"/> from the pool</returns>
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

		/// <summary>
		/// Returns a <see cref="HashSet{T}"/> to the pool
		/// </summary>
		/// <param name="set">Returned <see cref="HashSet{T}"/></param>
		public void Return(HashSet<T> set)
		{
			set.Clear();
			_pool.Enqueue(set);
		}
	}
}
