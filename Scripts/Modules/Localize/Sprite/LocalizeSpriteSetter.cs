using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UniRx;
using Extensions;

namespace Modules.Localize
{
	[RequireComponent(typeof(Image))]
	public sealed class LocalizeSpriteSetter : MonoBehaviour
	{
		//----- params -----

		//----- field -----

		[SerializeField]
		private string spriteGuid = null;
		[SerializeField]
		private string spriteName = null;

		private Image image = null;

		private Subject<Unit> onChangeAtlas = null;

		#if UNITY_EDITOR

		private string requireLoadAtlas = null;

		#endif

		//----- property -----

		public SpriteAtlas Atlas
		{
			get
			{
				var localizeAtlasManager = LocalizeAtlasManager.Instance;

				return localizeAtlasManager.GetSpriteAtlas(spriteGuid);
			}
		}

		//----- method -----

		void Awake()
		{
			var localizeAtlasManager = LocalizeAtlasManager.Instance;

			localizeAtlasManager.OnLoadAtlasAsObservable()
				.Subscribe(_ => OnAtlasChanged())
				.AddTo(this);
		}

		void OnEnable()
		{
			SetSprite();
		}

		private void SetSprite()
		{
			if (image == null)
			{
				image = UnityUtility.GetComponent<Image>(gameObject);
			}

			if (image == null){ return; }

			var localizeAtlasManager = LocalizeAtlasManager.Instance;

			var sprite = localizeAtlasManager.GetSprite(spriteGuid, spriteName);

			image.sprite = sprite;

			#if UNITY_EDITOR

			requireLoadAtlas = sprite == null ? localizeAtlasManager.GetAtlasFolderPath(spriteGuid) : null;

			#endif
		}

		private void OnAtlasChanged()
		{
			SetSprite();

			if (onChangeAtlas != null)
			{
				onChangeAtlas.OnNext(Unit.Default);
			}
		}

		public IObservable<Unit> OnChangeAtlasAsObservable()
		{
			return onChangeAtlas ?? (onChangeAtlas = new Subject<Unit>());
		}
	}
}
