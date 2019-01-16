﻿
using System.Linq;
using UnityEngine;
using UnityEditor.Callbacks;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public static class GameTextLoader
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            Reload();
        }

        public static void Reload()
        {
            if (!GameText.Exists)
            {
                GameText.CreateInstance();
            }

            var gameTextInfo = GameTextLanguage.GameTextInfos.ElementAtOrDefault(GameTextLanguage.Prefs.selection);

            if (gameTextInfo == null) { return; }
            
            GameText.Instance.Load(gameTextInfo.AssetPath);

            var gameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

            foreach (var gameObject in gameObjects)
            {
                var setter = gameObject.GetComponent<GameTextSetter>();

                if (setter != null)
                {
                    setter.ImportText();
                }
            }
        }
    }
}
