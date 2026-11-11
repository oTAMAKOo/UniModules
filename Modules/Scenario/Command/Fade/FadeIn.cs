
#if ENABLE_XLUA

using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	/// <summary>
	/// 画面フェードイン.
	/// </summary>
	[CSharpCallLua]
	public sealed class FadeIn : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "FadeIn"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		public Graphic TargetGraphic { get; set; }

		//----- method -----

		public async UniTask LuaCallback(float duration, float? endValue, string ease)
		{
			if (!endValue.HasValue)
			{
				endValue = 0f;
			}

			TweenCallback onUpdate = () =>
			{
				UnityUtility.SetActive(TargetGraphic, 0 < TargetGraphic.color.a);
			};

			if (string.IsNullOrEmpty(ease))
			{
				ease = Ease.OutQuad.ToString();
			}

			var tweener = TargetGraphic.DOFade(endValue.Value, duration)
				.OnUpdate(onUpdate);

			await TweenControl.Play(tweener, ease);
		}
	}
}

#endif