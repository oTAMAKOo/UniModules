using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using Modules.Tweening;

namespace Modules.Scenario
{
	public sealed class TweenControl : Singleton<TweenControl>
    {
        //----- params -----

        //----- field -----

		private TweenController tweenController = null;

        //----- property -----

		public float TimeScale
		{
			get { return tweenController.TimeScale; }
			set { tweenController.TimeScale = value; }
		}

        //----- method -----

		protected override void OnCreate()
		{
			tweenController = new TweenController();
		}

		public static async UniTask Play(Tweener tweener, string ease = null)
		{
			if (!string.IsNullOrEmpty(ease))
			{
				var easeType = EnumExtensions.FindByName(ease, Ease.Linear);

				tweener = tweener.SetEase(easeType);
			}

			await Instance.tweenController.Play(tweener);
		}
	}
}