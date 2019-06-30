
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System.Linq;
using System.Collections.Generic;
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
        private List<GameObject> ignoreTargets = new List<GameObject>();

        private List<Graphic> childGraphics = new List<Graphic>();
        private Color? prevColor = null;

        //----- property -----

        public Color ColorTint
        {
            get { return colorTint; }
            set
            {
                canvasRenderer.SetColor(value);
                ApplyColorForChildren(value);
            }
        }

        public GameObject[] IgnoreTargets
        {
            get { return ignoreTargets.ToArray(); }
            set { ignoreTargets = value.ToList(); }
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
            colorTint = canvasRenderer.GetColor();

            CollectChildGraphic();
            ApplyColorForChildren(colorTint);
        }

        /// <summary>
        /// 子階層のGraphicオブジェクトを収集します.
        /// </summary>
        public void CollectChildGraphic()
        {
            childGraphics.Clear();

            childGraphics = gameObject.Descendants().OfComponent<Graphic>()
                .Where(x => !ignoreTargets.Contains(x.gameObject))
                .ToList();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        private void ApplyColorForChildren(Color applyColor)
        {
            foreach (var graphic in childGraphics)
            {
                if (!UnityUtility.IsNull(graphic))
                {
                    graphic.canvasRenderer.SetColor(applyColor);
                    graphic.SetAllDirty();
                }
            }

            prevColor = applyColor;
        }

        void Update()
        {
            colorTint = canvasRenderer.GetColor();

            if (prevColor == null)
            {
                ApplyColorForChildren(colorTint);
            }
            else
            {
                if (prevColor.Value != colorTint)
                {
                    ApplyColorForChildren(colorTint);
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
