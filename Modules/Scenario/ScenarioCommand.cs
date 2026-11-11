
#if ENABLE_XLUA

using UnityEngine;
using Extensions;
using Modules.Lua.Command;

namespace Modules.Scenario
{
	public abstract class ScenarioCommand : ICommand
	{
		//----- params -----

		//----- field -----

		protected ScenarioController scenarioController = null;

		//----- property -----

		public abstract string LuaName { get; }

		public abstract string Callback { get; }

		//----- method -----

		public void Setup(ScenarioController scenarioController)
		{
			this.scenarioController = scenarioController;
		}

		public static T ToComponent<T>(object target) where T : Component
		{
			var gameObject = target as GameObject;

			if (gameObject != null)
			{
				return UnityUtility.GetComponent<T>(gameObject);
			}

			var component = target as Component;

			if (component != null)
			{
				return UnityUtility.GetComponent<T>(component);
			}
			
			return target as T;
		}
	}
}

#endif