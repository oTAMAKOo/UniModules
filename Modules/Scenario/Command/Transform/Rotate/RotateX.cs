
#if ENABLE_XLUA

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class RotateX : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "RotateX"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(object target, float endValue, float? duration, string ease)
		{
			var component = ToComponent<Component>(target);

			if (component == null){ return; }

			Action<float> updateValue = value =>
			{
				var eulerAngles = component.transform.localRotation.eulerAngles;

				eulerAngles.x = value;

				component.transform.localRotation = Quaternion.Euler(eulerAngles);
			};

			if (duration.HasValue)
			{
				var tweener = DOTween.To(() => component.transform.eulerAngles.x, x => updateValue(x), endValue, duration.Value);
				
				await TweenControl.Play(tweener, ease);
			}
			else
			{
				updateValue(endValue);
			}
		}
	}
}

#endif