using System;
using System.Collections.Concurrent;

namespace NorthwoodLib
{
	/// <summary>
	/// Queues <see cref="Action"/> and runs them on <see cref="Invoke"/>
	/// </summary>
	public sealed class ActionDispatcher
	{
		private readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

		/// <summary>
		/// Queues an <see cref="Action"/>
		/// </summary>
		/// <param name="action">Queued <see cref="Action"/></param>
		public void Dispatch(Action action)
		{
			_actionQueue.Enqueue(action);
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
}
