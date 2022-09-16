
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using Extensions;

namespace Modules.UI.DummyContent
{
	[ExecuteAlways]
	[RequireComponent(typeof(Image))]
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

		private Image image = null;

        //----- property -----

		public Image Image
		{
			get
			{
				return image ?? (image = UnityUtility.GetComponent<Image>(gameObject));
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

            if (Image == null) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            // 開発用画像が設定されていた箇所は空画像の時は非表示.

            Action onSpriteChanged = () =>
            {
                if (Image != null)
                {
                    Image.enabled = Image.sprite != null;
                }
            };

            Image.ObserveEveryValueChanged(x => x.sprite)
                .TakeUntilDisable(this)
                .Subscribe(_ => onSpriteChanged())
                .AddTo(this);

            onSpriteChanged.Invoke();
        }
    }
}