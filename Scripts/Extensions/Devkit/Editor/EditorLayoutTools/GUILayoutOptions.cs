
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Extensions.Devkit
{
    public sealed class GUILayoutOptions
    {
        public GUILayoutOption[] layoutOptions = null;

        public GUILayoutOptions()
        {
            this.layoutOptions = null;
        }

        public GUILayoutOptions(params GUILayoutOption[] options)
        {
            this.layoutOptions = options;
        }
    }
}
