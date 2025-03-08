
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Extensions;

namespace Modules.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    #if UNITY_5_2 || UNITY_5_3_OR_NEWER
    public class FlipGraphic : MonoBehaviour, IMeshModifier
    #else
    public class FlipGraphic : MonoBehaviour, IVertexModifier
    #endif
    {
        //----- params -----

        //----- field -----

        [SerializeField] 
        private bool horizontal = false;
        [SerializeField] 
        private bool veritical = false;

        private Graphic graphic = null;

        //----- property -----

        public bool Horizontal
        {
            get { return horizontal; }

            set
            {
                horizontal = value;
                SetVerticesDirty();
            }
        }
		
        public bool Vertical
        {
            get { return veritical; }

            set
            {
                veritical = value;
                SetVerticesDirty();
            }
        }

        //----- method -----

        #if UNITY_5_2 || UNITY_5_3_OR_NEWER

        public void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!enabled){ return; }
			
            var list = new List<UIVertex>();

            vertexHelper.GetUIVertexStream(list);
			
            ModifyVertices(list);
			
            vertexHelper.Clear();
            vertexHelper.AddUIVertexTriangleStream(list);
        }

        public void ModifyMesh(Mesh mesh)
        {
            if (!enabled){ return; }

            var list = new List<UIVertex>();

            using (var vertexHelper = new VertexHelper(mesh))
            {
                vertexHelper.GetUIVertexStream(list);
            }

            ModifyVertices(list);

            using (var vertexHelper2 = new VertexHelper())
            {
                vertexHelper2.AddUIVertexTriangleStream(list);
                vertexHelper2.FillMesh(mesh);
            }
        }

        #endif

        public void ModifyVertices(List<UIVertex> verts)
        {
            if (!enabled){ return; }

            var rt = transform as RectTransform;
			
            for (var i = 0; i < verts.Count; ++i)
            {
                var v = verts[i];

                var px = horizontal ? (v.position.x + (rt.rect.center.x - v.position.x) * 2) : v.position.x;
                var py = veritical ?  (v.position.y + (rt.rect.center.y - v.position.y) * 2) : v.position.y;
				
                v.position = new Vector3(px, py, v.position.z);
				
                verts[i] = v;
            }
        }

        private void SetVerticesDirty()
        {
            if (graphic == null)
            {
                graphic = UnityUtility.GetComponent<Graphic>(gameObject);
            }

            graphic.SetVerticesDirty();
        }

        #if UNITY_EDITOR

        protected void OnValidate()
        {
            SetVerticesDirty();
        }

        #endif
    }
}
