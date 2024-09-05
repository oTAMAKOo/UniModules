
#if ENABLE_XLUA

using XLua;
using Extensions;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class GetObject : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "GetObject"; } }

		public override string Callback 
        {
            get { return BuildCallName<GetObject>(nameof(LuaCallback)); }
        }

		//----- method -----

		public object LuaCallback(string key)
		{
			return scenarioController.ManagedObjects.Get(key);
		}
	}
}

#endif