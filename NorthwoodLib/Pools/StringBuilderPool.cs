using System;
using System.Collections.Concurrent;
using System.Text;

namespace NorthwoodLib.Pools;

/// <summary>
/// Returns pooled <see cref="StringBuilder"/>
/// </summary>
public sealed class StringBuilderPool : IPool<StringBuilder>
{
	/// <summary>
	/// Gets a shared <see cref="StringBuilderPool"/> instance
	/// </summary>
	public static readonly StringBuilderPool Shared = new();

	private readonly ConcurrentQueue<StringBuilder> _pool = new();

	/// <summary>
	/// Gives a pooled <see cref="StringBuilder"/>
	/// </summary>
	/// <returns><see cref="StringBuilder"/> from the pool</returns>
	public StringBuilder Rent()
	{
		return _pool.TryDequeue(out StringBuilder stringBuilder) ? stringBuilder : new StringBuilder(512);
	}

	/// <summary>
	/// Gives a pooled <see cref="StringBuilder"/> with provided capacity
	/// </summary>
	/// <param name="capacity">Requested capacity</param>
	/// <returns><see cref="StringBuilder"/> from the pool</returns>
	public StringBuilder Rent(int capacity)
	{
		if (_pool.TryDequeue(out StringBuilder stringBuilder))
		{
			if (stringBuilder.Capacity < capacity)
				stringBuilder.Capacity = capacity;
			return stringBuilder;
		}

		return new StringBuilder(Math.Max(capacity, 512));
	}

	/// <summary>
	/// Gives a pooled <see cref="StringBuilder"/> with initial content
	/// </summary>
	/// <param name="text">Initial content</param>
	/// <returns><see cref="StringBuilder"/> from the pool</returns>
	public StringBuilder Rent(string text)
	{
		if (_pool.TryDequeue(out StringBuilder stringBuilder))
		{
			stringBuilder.Append(text);
			return stringBuilder;
		}

		return new StringBuilder(text, 512);
	}

	/// <summary>
	/// Returns a <see cref="StringBuilder"/> to the pool
	/// </summary>
	/// <param name="stringBuilder">Returned <see cref="StringBuilder"/></param>
	public void Return(StringBuilder stringBuilder)
	{
		stringBuilder.Clear();
		_pool.Enqueue(stringBuilder);
	}

	/// <summary>
	/// Returns the content of a <see cref="StringBuilder"/> and returns it to the pool
	/// </summary>
	/// <param name="stringBuilder">Returned <see cref="StringBuilder"/></param>
	/// <returns>The content of the <see cref="StringBuilder"/></returns>
	public string ToStringReturn(StringBuilder stringBuilder)
	{
		string value = stringBuilder.ToString();
		stringBuilder.Clear();
		_pool.Enqueue(stringBuilder);
		return value;
	}
}
