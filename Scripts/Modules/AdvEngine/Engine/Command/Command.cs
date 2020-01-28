
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Lua;
using MoonSharp.Interpreter;

namespace Modules.AdvKit
{
    public abstract class Command : LifetimeDisposable
    {
        //----- params -----

        //----- field ----- 

        //----- property -----

        public abstract string CommandName { get; }

        public DynValue YieldWait { get { return LuaClass.YieldWait; } }

        //----- method -----

        public virtual void Initialize() { }

        public abstract object GetCommandDelegate();
    }
}