
using System;
using System.Diagnostics;

namespace Extensions
{
	public sealed class StopwatchScope : Scope
	{
		//----- params -----

		//----- field -----

		private Action<double> callback = null;

		private Stopwatch stopwatch = null;

		//----- property -----

		//----- method -----

		public StopwatchScope(Action<double> callback)
		{
			this.callback = callback;

			stopwatch = Stopwatch.StartNew();
		}

		protected override void CloseScope()
		{
			stopwatch.Stop();

			if (callback != null)
			{
				callback.Invoke(stopwatch.Elapsed.TotalMilliseconds);
			}
		}
	}
}