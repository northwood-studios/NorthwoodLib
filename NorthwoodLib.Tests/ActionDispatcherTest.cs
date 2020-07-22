using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NorthwoodLib.Tests
{
	public class ActionDispatcherTest
	{
		[Fact]
		public void InvokeTest()
		{
			bool executed = false;
			ActionDispatcher dispatcher = new ActionDispatcher();
			dispatcher.Dispatch(() => executed = true);
			dispatcher.Invoke();
			Assert.True(executed);
		}

		[Fact]
		public void OrderTest()
		{
			List<int> list = new List<int>();
			ActionDispatcher dispatcher = new ActionDispatcher();
			dispatcher.Dispatch(() => list.Add(1));
			dispatcher.Dispatch(() => list.Add(2));
			dispatcher.Dispatch(() => list.Add(3));
			dispatcher.Dispatch(() => list.Add(4));
			dispatcher.Dispatch(() => list.Add(5));
			dispatcher.Invoke();
			Assert.True(list.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
		}
	}
}
