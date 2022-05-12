
using System;
using System.Threading.Tasks;

namespace Extensions
{
    public static class TaskExtensions
    {
		public static async void Forget(this Task task, Action<Exception> exceptionHandler = null)
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				exceptionHandler?.Invoke(ex);
			}
		}

		public static async void Forget<T>(this Task<T> task, Action<Exception> exceptionHandler = null)
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				exceptionHandler?.Invoke(ex);
			}
		}
    }
}