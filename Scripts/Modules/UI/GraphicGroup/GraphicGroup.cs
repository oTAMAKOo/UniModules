
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System.Linq;
using Extensions;

namespace Modules.UI
{
    [ExecuteInEditMode]
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

            // Disable状態でcanvasRendererの色が戻ってしまう為復元.

            if (prevColor.HasValue)
            {
                ColorTint = prevColor.Value;
            }
            else
            {
                UpdateContents();
            }
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

            var color = canvasRenderer.GetColor() * colorTint;

            foreach (var graphic in childGraphics)
            {
                if (!UnityUtility.IsNull(graphic))
                {
                    graphic.canvasRenderer.SetColor(color);
                    graphic.SetAllDirty();
                }
            }

            prevColor = colorTint;
        }

        void Update()
        {
            if (prevColor == null)
            {
                ApplyColorForChildren();
            }
            else
            {
                if (prevColor.Value != colorTint)
                {
                    ApplyColorForChildren();
                }
            }
        }

        // ※ 子階層から別階層に移動した際のイベントがOnTransformChildrenChanged()で取得できない為.
        //    そういうケースはUpdateContents関数を自前で叩くことで対応する.
        void OnTransformChildrenChanged()
        {
            UpdateContents();
        }
    }
}
