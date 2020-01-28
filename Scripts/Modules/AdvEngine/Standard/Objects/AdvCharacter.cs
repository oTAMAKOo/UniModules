
#if ENABLE_MOONSHARP

using UnityEngine;
using Extensions;
using Modules.Dicing;

namespace Modules.AdvKit.Standard
{
    public sealed class AdvCharacter : AdvObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private DicingImage dicingImage = null;

        private string characterName = null;
        private string resourcePath = null;

        //----- property -----

        public string CharacterName { get { return characterName; } }

        public DicingImage DicingImage { get { return dicingImage; } }

        //----- method -----

        public void Setup(string characterName, string resourcePath)
        {
            this.characterName = characterName;
            this.resourcePath = resourcePath;
        }

        public void Show(int face)
        {
            var advEngine = AdvEngine.Instance;

            dicingImage.DicingTexture = advEngine.Resource.Get<DicingTexture>(resourcePath);

            dicingImage.PatternName = face.ToString();

            dicingImage.SetNativeSize();

            UnityUtility.SetActive(gameObject, true);
        }

        public void Hide()
        {
            dicingImage.DicingTexture = null;

            UnityUtility.SetActive(gameObject, false);
        }
    }
}

#endif
