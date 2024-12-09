
using System;

namespace Modules.Master
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public abstract class CustomDataAttribute : Attribute
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public string Name { get; private set; }

        public int Priority { get; private set; }

        //----- method -----

        public CustomDataAttribute(string name, int priority = 0)
        {
            this.Name = name;
            this.Priority = priority;
        }

        public abstract Type GetDataType();

        public abstract object GetCustomData(object record, object value);
    }
}