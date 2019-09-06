
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor.Callbacks;
using Extensions;
using System.Security.Cryptography;
using Modules.GameText;
using Modules.GameText.Components;

namespace Modules.UI.Extension
{
    public partial class UIText
    {
        //----- params -----

        private const string AESKey = "5k7DpFGc9A9iRaLkv2nCdMxCmjHFzxOX";
        private const string AESIv = "FiEA3x1fs8JhK9Tp";

        public const char DevelopmentMark = '#';

        //----- field -----
        
        #pragma warning disable 0414

        [SerializeField, HideInInspector]
        private string developmentText = null;

        #pragma warning restore 0414

        private static AesManaged aesManaged = null;

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            var allObjects = UnityUtility.FindObjectsOfType<UIText>();

            foreach (var item in allObjects)
            {
                item.ImportText();
            }
        }

        public string GetDevelopmentText()
        {
            if (aesManaged == null)
            {
                aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);
            }

            if (developmentText == null) { return string.Empty; }

            return string.Format("{0}{1}", DevelopmentMark, developmentText.Decrypt(aesManaged));
        }

        public void SetDevelopmentText(string text)
        {
            if (aesManaged == null)
            {
                aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);
            }

            developmentText = text == null ? string.Empty : text.Encrypt(aesManaged);
        }

        public void ImportText()
        {
            if (string.IsNullOrEmpty(developmentText)) { return; }

            var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(gameObject);

            if (gameTextSetter == null || gameTextSetter.Category == GameTextCategory.None)
            {
                text = GetDevelopmentText();
            }
        }
    }
}

#endif
