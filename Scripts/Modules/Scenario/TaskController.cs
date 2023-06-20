
#if ENABLE_XLUA

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Scenario
{
    public sealed class TaskController
    {
        //----- params -----

        //----- field -----

        private Dictionary<string, List<UniTask>> tasks = null;

        //----- property -----

        //----- method -----

        public TaskController()
        {
            tasks = new Dictionary<string, List<UniTask>>();
        }

        public void AddTask(string taskName, UniTask task)
        {
            var list = tasks.GetValueOrDefault(taskName);

            if (list == null)
            {
                list = new List<UniTask>();

                tasks.Add(taskName, list);
            }

            list.Add(task);
        }

        public void RemoveTask(string taskName)
        {
            if (tasks.ContainsKey(taskName))
            {
                tasks.Remove(taskName);
            }
        }

        public async UniTask ExecuteTask(string taskName)
        {
            var list = tasks.GetValueOrDefault(taskName);

            if (list.IsEmpty()) { return; }
            
            await UniTask.WhenAll(list);

            RemoveTask(taskName);
        }

        public void Clear()
        {
            tasks.Clear();
        }
    }
}

#endif