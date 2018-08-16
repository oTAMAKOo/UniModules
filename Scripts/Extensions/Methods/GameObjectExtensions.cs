﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Extensions
{
    public static class GameObjectExtensions
    {
        public static void ResetTransform(this GameObject gameObject, bool localPosition = true, bool localRotation = true, bool localScale = true)
        {
            gameObject.transform.Reset(localPosition, localRotation, localScale);
        }
    }
}