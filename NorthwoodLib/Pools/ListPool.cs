using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NorthwoodLib.Pools;

/// <summary>
/// Returns pooled <see cref="List{T}"/>
/// </summary>
/// <typeparam name="T">Element type</typeparam>
public sealed class ListPool<T> : IPool<List<T>>
{
	/// <summary>
	/// Gets a shared <see cref="ListPool{T}"/> instance
	/// </summary>
	public static readonly ListPool<T> Shared = new();

	private readonly ConcurrentQueue<List<T>> _pool = new();

	/// <summary>
	/// Gives a pooled <see cref="List{T}"/>
	/// </summary>
	/// <returns><see cref="List{T}"/> from the pool</returns>
	public List<T> Rent()
	{
		return _pool.TryDequeue(out List<T> list) ? list : new List<T>(512);
	}

	/// <summary>
	/// Gives a pooled <see cref="List{T}"/> with provided capacity
	/// </summary>
	/// <param name="capacity">Requested capacity</param>
	/// <returns><see cref="List{T}"/> from the pool</returns>
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

	/// <summary>
	/// Gives a pooled <see cref="List{T}"/> with initial content
	/// </summary>
	/// <param name="enumerable">Initial content</param>
	/// <returns><see cref="List{T}"/> from the pool</returns>
	public List<T> Rent(IEnumerable<T> enumerable)
	{
		if (_pool.TryDequeue(out List<T> list))
		{
			list.AddRange(enumerable);
			return list;
		}

		return [..enumerable];
	}

	/// <summary>
	/// Returns a <see cref="List{T}"/> to the pool
	/// </summary>
	/// <param name="list">Returned <see cref="List{T}"/></param>
	public void Return(List<T> list)
	{
		list.Clear();
		_pool.Enqueue(list);
	}
}
