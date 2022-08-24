
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class RotateY : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "RotateY"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(object target, float endValue, float? duration, string ease)
		{
			var component = ToComponent<Component>(target);

			if (component == null){ return; }

			Action<float> updateValue = value =>
			{
				var eulerAngles = component.transform.localRotation.eulerAngles;

				eulerAngles.y = value;

				component.transform.localRotation = Quaternion.Euler(eulerAngles);
			};

			if (duration.HasValue)
			{
				var tweener = DOTween.To(() => component.transform.eulerAngles.y, x => updateValue(x), endValue, duration.Value);

				await TweenControl.Play(tweener, ease);
			}
			else
			{
				updateValue(endValue);
			}
		}
	}
}