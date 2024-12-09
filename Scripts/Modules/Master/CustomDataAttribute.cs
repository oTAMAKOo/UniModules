
using System;

namespace Modules.Master
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public abstract class CustomDataAttribute : Attribute
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public int Priority { get; private set; }

        //----- method -----

        public CustomDataAttribute(int priority = 0)
        {
            this.Priority = priority;
        }

        public abstract string GetTag();

        public abstract Type GetDataType();

        public abstract object GetCustomData(object record, object value);
    }
}