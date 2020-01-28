
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Lua;

namespace Modules.AdvKit
{
    public abstract class CommandController : LuaClass
    {
        //----- params -----

        //----- field -----

        private Dictionary<Type, Command> commandDictionary = null;

        //----- property -----

        protected abstract Type[] CommandTypes { get; }

        //----- method -----

        public CommandController()
        {
            commandDictionary = new Dictionary<Type, Command>();
        }

        public override void RegisterCommand()
        {
            foreach (var commandType in CommandTypes)
            {
                var advCommand = Activator.CreateInstance(commandType) as Command;

                command[advCommand.CommandName] = advCommand.GetCommandDelegate();

                commandDictionary.Add(commandType, advCommand);
            }
        }

        public IEnumerable<Command> GetAllCommands()
        {
            return commandDictionary.Values.Where(x => x != null);
        }

        public T GetCommandClass<T>() where T : Command
        {
            return commandDictionary.GetValueOrDefault(typeof(T)) as T;
        }
    }
}

#endif
