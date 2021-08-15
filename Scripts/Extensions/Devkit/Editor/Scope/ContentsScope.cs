﻿﻿
using UnityEngine;

namespace Extensions.Devkit
{
    public sealed class ContentsScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public ContentsScope()
        {
            EditorGUIContentLayout.BeginContents();
        }

        protected override void CloseScope()
        {
            EditorGUIContentLayout.EndContents();
        }
    }
}
