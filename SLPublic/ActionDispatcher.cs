using System;
using System.Collections.Concurrent;

namespace SLPublic
{
	public sealed class ActionDispatcher
	{
		private readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

		public void Dispatch(Action action)
		{
			_actionQueue.Enqueue(action);
		}

		public void Invoke()
		{
			while (_actionQueue.TryDequeue(out Action action))
				action();
		}
	}
}
