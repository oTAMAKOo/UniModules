﻿﻿
using UnityEngine;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;

namespace Extensions
{
    public static class FloatExtensions
    {
        public static bool ValueInRange(this float value, float min, float max)
        {
            return (value >= min) && (value <= max);
        }
    }
}