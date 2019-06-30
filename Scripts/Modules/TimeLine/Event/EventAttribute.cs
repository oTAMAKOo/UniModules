
using System;

namespace Modules.TimeLine
{
    /// <summary>
    /// TimeLineイベント関数として登録する為の属性.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TimeLineEventAttribute : Attribute { }
}
