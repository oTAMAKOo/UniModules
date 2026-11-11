
using UnityEngine;
using System;
using R3;
using Extensions;

namespace Modules.Renderer2D.DummyContent
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed partial class DummySprite : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        #pragma warning disable 0414

        // 開発アセット登録用.

        [SerializeField, HideInInspector]
        private string assetGuid = null;
        [SerializeField, HideInInspector]
        private string spriteId = null;

        #pragma warning restore 0414

        private SpriteRenderer spriteRenderer = null;

        //----- property -----

        public SpriteRenderer SpriteRenderer
        {
            get
            {
                return spriteRenderer ?? (spriteRenderer = UnityUtility.GetComponent<SpriteRenderer>(gameObject));
            }
        }

        //----- method -----

        void OnEnable()
        {
            StartEmptySpriteCheck();

            #if UNITY_EDITOR

            ApplyDummyAsset();

            #endif
        }

        void OnDisable()
        {
            #if UNITY_EDITOR

            DeleteCreatedAsset();

            #endif
        }

        private void StartEmptySpriteCheck()
        {
            if (!Application.isPlaying) { return; }

            if (SpriteRenderer == null) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            // 開発用画像が設定されていた箇所は空画像の時は非表示.

            Action onSpriteChanged = () =>
            {
                if (SpriteRenderer != null)
                {
                    SpriteRenderer.enabled = SpriteRenderer.sprite != null;
                }
            };

            SpriteRenderer.ObserveEveryValueChanged(x => x.sprite)
                .TakeUntilDisable(this)
                .Subscribe(_ => onSpriteChanged())
                .AddTo(this);

            onSpriteChanged.Invoke();
        }
    }
}
