﻿
#if ENABLE_CRIWARE
﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.CriWare.Editor;

namespace Modules.MovieManagement.Editor
{
    [CustomEditor(typeof(MovieConfig))]
    public class MovieConfigInspector : CriAssetConfigInspectorBase
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDirectoryInspector(serializedObject);

            EditorGUILayout.Separator();
        }
    }
}

#endif