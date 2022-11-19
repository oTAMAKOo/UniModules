
#if ENABLE_XLUA

using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class Shake : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Shake"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(object target, float duration, Vector2 strength, bool? sync, int? vibrato, float? randomness)
		{
			var component = ToComponent<Component>(target);

			if (component == null){ return; }

			if (!sync.HasValue)
			{
				sync = false;
			}

			if (!vibrato.HasValue)
			{
				vibrato = 10;
			}

			if (!randomness.HasValue)
			{
				randomness = 90f;
			}

			var tweener = component.transform.DOShakePosition(duration, strength, vibrato.Value, randomness.Value);

			if (sync.Value)
			{
				await TweenControl.Play(tweener);
			}
			else
			{
				TweenControl.Play(tweener).Forget();
			}
		}
	}
}

#endif