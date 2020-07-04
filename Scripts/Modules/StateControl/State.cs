
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.StateControl
{
    public abstract class StateArgument { }

    public sealed class State<T> where T : Enum
    {
        //----- params -----

        //----- field -----
        
        private SortedDictionary<int, List<Func<object, IEnumerator>>> enterFunctions = null;

        private SortedDictionary<int, List<Func<T, IEnumerator>>> exitFunctions = null;
        
        //----- property -----

        public T Type { get; private set; }

        //----- method -----

        public State(T state)
        {
            Type = state;

            enterFunctions = new SortedDictionary<int, List<Func<object, IEnumerator>>>();
            exitFunctions = new SortedDictionary<int, List<Func<T, IEnumerator>>>();
        }

        /// <summary> 登録済みのイベントをクリア </summary>
        public void ClearFunctions()
        {
            enterFunctions.Clear();
            exitFunctions.Clear();
        }

        /// <summary> 開始イベント追加 </summary>
        public void AddEnterFunction(Func<IEnumerator> function, int priority = 0)
        {
            var list = enterFunctions.GetOrAdd(priority, i => new List<Func<object, IEnumerator>>());

            Func<object, IEnumerator> enterFunction = x =>
            {
                return function.Invoke();
            };

            list.Add(enterFunction);
        }

        /// <summary> 開始イベント追加 </summary>
        public void AddEnterFunction<TArgument>(Func<TArgument, IEnumerator> function, int priority = 0) where TArgument : StateArgument
        {
            var list = enterFunctions.GetOrAdd(priority, i => new List<Func<object, IEnumerator>>());

            Func<object, IEnumerator> enterFunction = x =>
            {
                TArgument argument = null;

                if (x != null)
                {
                    argument = x as TArgument;

                    if (argument == null)
                    {
                        var message = string.Format("The argument type is invalid.\nRequire Type: {0}\nTArgument Type:{1}", typeof(TArgument).FullName, x.GetType().FullName);

                        throw new ArgumentException(message);
                    }
                }

                return function.Invoke(argument);
            };

            list.Add(enterFunction);
        }

        /// <summary> 終了イベント追加 </summary>
        public void AddExitFunction(Func<T, IEnumerator> function, int priority = 0)
        {
            var list = exitFunctions.GetOrAdd(priority, i => new List<Func<T, IEnumerator>>());

            list.Add(function);
        }

        public IReadOnlyList<List<Func<object, IEnumerator>>> GetEnterFunctions()
        {
            return enterFunctions.Values.ToArray();
        }

        public IReadOnlyList<List<Func<T, IEnumerator>>> GetExitFunctions()
        {
            return exitFunctions.Values.ToArray();
        }
    }
}
