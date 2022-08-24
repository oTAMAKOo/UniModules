
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	/// <summary>
	/// 画面フェードアウト.
	/// </summary>
	[CSharpCallLua]
	public sealed class FadeOut : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "FadeOut"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		public Graphic TargetGraphic { get; set; }
		
		//----- method -----

		public async UniTask LuaCallback(float duration, float? endValue, string ease)
		{
			if (!endValue.HasValue)
			{
				endValue = 1f;
			}

			if (string.IsNullOrEmpty(ease))
			{
				ease = Ease.OutQuad.ToString();
			}

			TweenCallback onUpdate = () =>
			{
				UnityUtility.SetActive(TargetGraphic, 0 < TargetGraphic.color.a);
			};

			var tweener = TargetGraphic.DOFade(endValue.Value, duration)
				.OnUpdate(onUpdate);

			await TweenControl.Play(tweener, ease);
		}
	}
}