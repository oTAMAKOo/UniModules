
using UnityEditor.Callbacks;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public static class GameTextLoader
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public static bool IsLoaded { get; private set; }

        //----- method -----

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            Observable.TimerFrame(30).First().Subscribe(_ => Reload());
        }

        public static void Reload()
        {
            var gameText = GameText.Instance;

            var gameTextInfo = GameTextLanguage.Infos.ElementAtOrDefault(GameTextLanguage.Prefs.selection);

            if (gameTextInfo == null) { return; }

            gameText.LoadFromResources(gameTextInfo.AssetName);
            
            var gameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

            foreach (var gameObject in gameObjects)
            {
                var setter = gameObject.GetComponent<GameTextSetter>();

                if (setter != null)
                {
                    setter.ImportText();
                }
            }

            IsLoaded = true;
        }
    }
}
