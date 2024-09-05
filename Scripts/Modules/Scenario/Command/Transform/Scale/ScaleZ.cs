
#if ENABLE_XLUA

using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class ScaleZ : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "ScaleZ"; } }

        public override string Callback 
        {
            get { return BuildCallName<ScaleZ>(nameof(LuaCallback)); }
        }

		//----- method -----

		public async UniTask LuaCallback(object target, float endValue, float? duration, string ease)
		{
			var component = ToComponent<Component>(target);

			if (component == null){ return; }

			if (duration.HasValue)
			{
				var tweener = component.transform.DOScaleZ(endValue, duration.Value);
				
				await TweenControl.Play(tweener, ease);
			}
			else
			{
				component.transform.localScale = Vector.SetZ(component.transform.localScale, endValue);
			}
		}
	}
}

#endif