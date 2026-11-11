
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Modules.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GraphicCast : Graphic
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            vh.Clear();
        }

        #if UNITY_EDITOR

        [CustomEditor(typeof(GraphicCast))]
        private sealed class GraphicCastEditor : Editor
        {
            public override void OnInspectorGUI(){}
        }

        #endif

    }
}
