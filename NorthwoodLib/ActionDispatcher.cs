using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace NorthwoodLib;

/// <summary>
/// Queues <see cref="Action"/> and runs them on <see cref="Invoke"/>
/// </summary>
public sealed class ActionDispatcher
{
	private readonly ConcurrentQueue<Action> _actionQueue = new();

	/// <summary>
	/// Queues an <see cref="Action"/>
	/// </summary>
	/// <param name="action">Queued <see cref="Action"/></param>
	public void Dispatch(Action action)
	{
		_actionQueue.Enqueue(action);
	}

	/// <summary>
	/// Queues an <see cref="Action"/> and waits for it to finish
	/// </summary>
	/// <param name="action">Queued <see cref="Action"/></param>
	/// <param name="sleepTime">Finish check sleep time</param>
	public void Wait(Action action, int sleepTime)
	{
		bool finished = false;
		_actionQueue.Enqueue(() =>
		{
			action();
			Volatile.Write(ref finished, true);
		});
		while (!Volatile.Read(ref finished))
			Thread.Sleep(sleepTime);
	}

	/// <summary>
	/// Queues a collection of <see cref="Action"/>s and waits for all of them to finish
	/// </summary>
	/// <param name="actions">Queued collection of <see cref="Action"/>s</param>
	/// <param name="sleepTime">Finish check sleep time</param>
	public void Wait(IEnumerable<Action> actions, int sleepTime)
	{
		bool finished = false;
		_actionQueue.Enqueue(() =>
		{
			foreach (Action action in actions)
				action();
			Volatile.Write(ref finished, true);
		});
		while (!Volatile.Read(ref finished))
			Thread.Sleep(sleepTime);
	}

	/// <summary>
	/// Queues a <see cref="Func{TResult}"/> and waits for it to finish
	/// </summary>
	/// <typeparam name="T">Function return type</typeparam>
	/// <param name="func">Queued <see cref="Func{TResult}"/></param>
	/// <param name="sleepTime">Finish check sleep time</param>
	/// <returns>Value returned by <see param="func"/></returns>
	public T Wait<T>(Func<T> func, int sleepTime)
	{
		T result = default;
		bool finished = false;
		_actionQueue.Enqueue(() =>
		{
			result = func();
			Volatile.Write(ref finished, true);
		});
		while (!Volatile.Read(ref finished))
			Thread.Sleep(sleepTime);
		return result;
	}

	/// <summary>
	/// Runs all scheduled <see cref="Action"/>
	/// </summary>
	public void Invoke()
	{
		while (_actionQueue.TryDequeue(out Action action))
			action();
	}
}
