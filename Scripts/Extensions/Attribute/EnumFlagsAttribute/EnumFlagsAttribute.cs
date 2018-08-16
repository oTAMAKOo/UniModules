
using UnityEngine;
using System;

namespace Extensions
{
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public sealed class EnumFlagsAttribute : PropertyAttribute
    {
    
    }
}