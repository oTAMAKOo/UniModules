
using UnityEngine;
using UnityEngine.UI;
using System;
using Extensions;
using Extensions.Serialize;

namespace Modules.Localize
{
	[RequireComponent(typeof(Image))]
    public abstract class BuiltinLocalizeSpriteSetter : MonoBehaviour
    {
        //----- params -----

		[Serializable]
		public sealed class SpriteDictionary : SerializableDictionary<Enum, Sprite> { }

        //----- field -----

		[SerializeField]
		private SpriteDictionary spriteDictionary = null;

		private Image image = null;

        //----- property -----

		public abstract Type LanguageType { get; }

		protected abstract Enum CurrentLanguage { get; }

        //----- method -----

		void OnEnable()
		{
			if (spriteDictionary == null){ return; }

			if (image == null)
			{
				image = UnityUtility.GetComponent<Image>(gameObject);
			}

			if (image != null)
			{
				var sprite = spriteDictionary.GetValueOrDefault(CurrentLanguage);

				image.sprite = sprite;
			}
		}
    }
}