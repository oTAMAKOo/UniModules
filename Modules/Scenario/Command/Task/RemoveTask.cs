
#if ENABLE_XLUA

using XLua;

namespace Modules.Scenario.Command
{
    [CSharpCallLua]
    public sealed class RemoveTask : ScenarioCommand
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string LuaName { get { return "RemoveTask"; } }

        public override string Callback { get { return nameof(LuaCallback); } }

        //----- method -----

        public void LuaCallback(string taskName)
        {
            var taskController = scenarioController.TaskController;

            taskController.RemoveTask(taskName);
        }
    }
}

#endif