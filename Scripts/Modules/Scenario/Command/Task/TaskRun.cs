
#if ENABLE_XLUA

using Cysharp.Threading.Tasks;
using XLua;

namespace Modules.Scenario.Command
{
    [CSharpCallLua]
    public sealed class TaskRun : ScenarioCommand
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string LuaName { get { return "TaskRun"; } }

        public override string Callback 
        {
            get { return BuildCallName<TaskRun>(nameof(LuaCallback)); }
        }

        //----- method -----

        public async UniTask LuaCallback(string taskName)
        {
            var taskController = scenarioController.TaskController;

            await taskController.ExecuteTask(taskName);
        }
    }
}

#endif