
using System;

namespace Modules.TimeLine
{
    /// <summary>
    /// TimeLineイベント関数として登録する為の属性.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TimeLineEventAttribute : Attribute { }
}
