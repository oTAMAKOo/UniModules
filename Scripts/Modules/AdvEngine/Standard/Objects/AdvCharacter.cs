
using UnityEngine;
using Extensions;
using Modules.PatternTexture;

namespace Modules.AdvKit.Standard
{
    public sealed class AdvCharacter : AdvObject
    {
        #if ENABLE_MOONSHARP

        //----- params -----

        //----- field -----

        [SerializeField]
        private PatternImage patternImage = null;

        private string characterName = null;
        private string resourcePath = null;

        //----- property -----

        public string CharacterName { get { return characterName; } }

        public PatternImage PatternImage { get { return patternImage; } }

        //----- method -----

        public void Setup(string characterName, string resourcePath)
        {
            this.characterName = characterName;
            this.resourcePath = resourcePath;
        }

        public void Show(string patternName)
        {
            var advEngine = AdvEngine.Instance;

            patternImage.PatternTexture = advEngine.Resource.Get<PatternTexture.PatternTexture>(resourcePath);

            patternImage.PatternName = patternName;

            patternImage.SetNativeSize();

            UnityUtility.SetActive(gameObject, true);
        }

        public void Hide()
        {
            patternImage.PatternTexture = null;

            UnityUtility.SetActive(gameObject, false);
        }

        #endif
    }
}
