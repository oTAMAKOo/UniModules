
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public sealed class GlowGraphic : MonoBehaviour
    {
        //----- params -----

        /// <summary> 発光色のシェーダーパラメータ </summary>
        private static readonly int PROPERTY_EMISSION_COLOR = Shader.PropertyToID("_EmissionColor");

        //----- field -----

        /// <summary> 発光色（HDR） </summary>
        [ColorUsage(false, true)]
        public Color emissionColor = Color.white;

        private Material material = null;

        private bool initialized = false;

        private static Shader uiGlowShader = null;

        //----- property -----

        //----- method -----

        void Awake()
        {
            Initialize();
        }

        /// <summary> 初期化 </summary>
        private void Initialize()
        {
            if (initialized){ return; }

            // UI/Glowシェーダーでマテリアル生成
            if (uiGlowShader == null)
            {
                uiGlowShader = Shader.Find("Custom/uGUI/Glow");
            }

            if (uiGlowShader != null)
            {
                if (material == null)
                {
                    var graphic = UnityUtility.GetComponent<Graphic>(gameObject);

                    material = new Material(uiGlowShader);

                    material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

                    graphic.material = material;

                    if (Application.isPlaying)
                    {
                        this.ObserveEveryValueChanged(x => x.emissionColor)
                            .Subscribe(_ => ApplyParameter())
                            .AddTo(this);
                    }
                }
            }

            initialized = true;
        }

        private void ApplyParameter()
        {
            Initialize();

            if (material != null)
            {
                material.SetColor(PROPERTY_EMISSION_COLOR, emissionColor);
            }
        }
        
        void OnDestroy()
        {
            var graphic = UnityUtility.GetComponent<Graphic>(gameObject);

            graphic.material = null;

            UnityUtility.SafeDelete(material);
        }

        #if UNITY_EDITOR
        
        void OnValidate()
        {
            if (Application.isPlaying){ return; }

            ApplyParameter();
        }

        #endif
    }
}