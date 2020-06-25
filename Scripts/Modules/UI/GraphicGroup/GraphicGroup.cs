
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System.Linq;
using Extensions;

namespace Modules.UI
{
    [ExecuteAlways]
    public sealed class GraphicGroup : Graphic
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Color colorTint = Color.white;
        [SerializeField]
        private GameObject[] ignoreTargets = null;

        private Graphic[] childGraphics = null;

        private Color? prevColor = null;

        //----- property -----

        public Color ColorTint
        {
            get { return colorTint; }
            set
            {
                colorTint = value;

                ApplyColorForChildren();
            }
        }

        public GameObject[] IgnoreTargets
        {
            get { return ignoreTargets ?? new GameObject[0]; }
            set { ignoreTargets = value; }
        }

        //----- method -----

        protected override void Awake()
        {
            base.Awake();

            UpdateContents();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateContents();
        }

        [ContextMenu("UpdateContents")]
        public void UpdateContents()
        {
            CollectChildGraphic();

            ApplyColorForChildren();
        }

        /// <summary>
        /// 子階層のGraphicオブジェクトを収集します.
        /// </summary>
        public void CollectChildGraphic()
        {
            childGraphics = gameObject.Descendants().OfComponent<Graphic>()
                .Where(x => !ignoreTargets.Contains(x.gameObject))
                .ToArray();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        private void ApplyColorForChildren()
        {
            if (childGraphics == null)
            {
                CollectChildGraphic();
            }

            var applyColor = GetApplyColor();

            foreach (var graphic in childGraphics)
            {
                if (UnityUtility.IsNull(graphic)) { continue; }

                graphic.canvasRenderer.SetColor(applyColor);
                graphic.SetAllDirty();
            }

            prevColor = applyColor;
        }

        void Update()
        {
            var applyColor = GetApplyColor();

            if (prevColor == null)
            {
                ApplyColorForChildren();
            }
            else
            {
                if (prevColor.Value != applyColor)
                {
                    ApplyColorForChildren();
                }
            }
        }

        private Color GetApplyColor()
        {
            return canvasRenderer.GetColor() * colorTint;
        }

        // ※ 子階層から別階層に移動した際のイベントがOnTransformChildrenChanged()で取得できない為.
        //    そういうケースはUpdateContents関数を自前で叩くことで対応する.
        void OnTransformChildrenChanged()
        {
            UpdateContents();
        }
    }
}
