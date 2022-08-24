
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Extensions
{
	public static class UniTaskExtensions
	{
		public static void Forget(this UniTask task, Component component)
		{
			task.ToObservable().Subscribe().AddTo(component);
		}

		public static void Forget(this UniTask task, GameObject gameObject)
		{
			task.ToObservable().Subscribe().AddTo(gameObject);
		}
	}
}
