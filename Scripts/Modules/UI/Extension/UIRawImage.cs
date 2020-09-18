
using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public abstract partial class UIRawImage : UIComponent<RawImage>
    {
        //----- params -----

        //----- field -----

        // 開発アセット登録用.

        [SerializeField, HideInInspector]
        private string assetGuid = null;

        //----- property -----

        public RawImage RawImage { get { return component; } }

        public Texture texture
        {
            get { return component.texture; }
            set { component.texture = value; }
        }

        //----- method -----

        void OnEnable()
        {
            StartEmptyTextureCheck();

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

        private void StartEmptyTextureCheck()
        {
            if (!Application.isPlaying) { return; }

            if (RawImage == null) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            // 開発用画像が設定されていた箇所は空画像の時は非表示.

            Action onTextureChanged = () =>
            {
                if (RawImage != null)
                {
                    RawImage.enabled = texture != null;
                }
            };

            RawImage.ObserveEveryValueChanged(x => x.texture)
                .TakeUntilDisable(this)
                .Subscribe(_ => onTextureChanged())
                .AddTo(this);

            onTextureChanged.Invoke();
        }
    }
}
