using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace NorthwoodLib.Tests
{
	public class ActionDispatcherTest
	{
		[Fact]
		public void InvokeTest()
		{
			bool executed = false;
			ActionDispatcher dispatcher = new();
			dispatcher.Dispatch(() => executed = true);
			dispatcher.Invoke();
			Assert.True(executed);
		}

		[Fact]
		public void OrderTest()
		{
			List<int> list = new();
			ActionDispatcher dispatcher = new();
			dispatcher.Dispatch(() => list.Add(1));
			dispatcher.Dispatch(() => list.Add(2));
			dispatcher.Dispatch(() => list.Add(3));
			dispatcher.Dispatch(() => list.Add(4));
			dispatcher.Dispatch(() => list.Add(5));
			dispatcher.Invoke();
			Assert.True(list.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
		}

		[Fact]
		public void WaitActionTest()
		{
			bool threadRunning = true;
			ActionDispatcher dispatcher = new();
			new Thread(() =>
				{
					// ReSharper disable once AccessToModifiedClosure
					while (Volatile.Read(ref threadRunning))
					{
						dispatcher.Invoke();
						Thread.Sleep(15);
					}
				})
			{ IsBackground = true }.Start();
			bool done = false;
			dispatcher.Wait(() => done = true, 5);
			Volatile.Write(ref threadRunning, false);
			Assert.True(done);
		}

		[Fact]
		public void WaitFuncTest()
		{
			bool threadRunning = true;
			ActionDispatcher dispatcher = new();
			new Thread(() =>
				{
					// ReSharper disable once AccessToModifiedClosure
					while (Volatile.Read(ref threadRunning))
					{
						dispatcher.Invoke();
						Thread.Sleep(15);
					}
				})
			{ IsBackground = true }.Start();
			bool done = dispatcher.Wait(() => true, 5);
			Volatile.Write(ref threadRunning, false);
			Assert.True(done);
		}

		[Fact]
		public void WaitArrayTest()
		{
			bool threadRunning = true;
			ActionDispatcher dispatcher = new();
			new Thread(() =>
			{
				// ReSharper disable once AccessToModifiedClosure
				while (Volatile.Read(ref threadRunning))
				{
					dispatcher.Invoke();
					Thread.Sleep(15);
				}
			})
			{ IsBackground = true }.Start();
			List<int> list = new();
			dispatcher.Wait(new[]
			{
				() => list.Add(1),
				() => list.Add(2),
				() => list.Add(3),
				() => list.Add(4),
				() => list.Add(5)
			}, 5);
			Volatile.Write(ref threadRunning, false);
			Assert.True(list.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
		}
	}
}
