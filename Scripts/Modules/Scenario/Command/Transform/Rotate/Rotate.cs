
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class Rotate : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Rotate"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(object target, Vector3 endValue, float? duration, string ease)
		{
			var component = ToComponent<Component>(target);

			if (component == null){ return; }

			if (duration.HasValue)
			{
				var tweener = component.transform.DOLocalRotate(endValue, duration.Value);
				
				await TweenControl.Play(tweener, ease);
			}
			else
			{
				component.transform.localRotation = Quaternion.Euler(endValue);
			}
		}
	}
}