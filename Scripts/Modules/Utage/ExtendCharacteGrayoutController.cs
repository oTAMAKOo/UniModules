
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
    public class ExtendCharacteGrayoutController : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private AdvEngine engine = null;
        [SerializeField]
        private AdvCharacterGrayOutController characterGrayOutController = null;

        private HashSet<string> grayoutControllers = null;

        //----- property -----

        //----- method -----

        void Awake()
        {
            grayoutControllers = new HashSet<string>();

            if (engine == null)
            {
                engine = UnityUtility.FindObjectOfType<AdvEngine>();
            }

            if (characterGrayOutController == null)
            {
                characterGrayOutController = UnityUtility.FindObjectOfType<AdvCharacterGrayOutController>();
            }

            // テキストに変更があった場合.
            if (engine != null)
            {
                engine.Page.OnBeginText.AddListener(OnBeginText);
            }
        }
        
        private void OnBeginText(AdvPage page)
        {
            // 毎回クリア.
            foreach (var item in grayoutControllers)
            {
                characterGrayOutController.NoGrayoutCharacters.Remove(item);
            }

            grayoutControllers.Clear();

            // 表示がないキャラは既存の全キャラをグレーアウト対象外にする.
            var desableGrayout = page.CharacterInfo == null || page.CharacterInfo.Graphic == null;

            if (desableGrayout)
            {
                var allLayers = engine.GraphicManager.CharacterManager.AllGraphicsLayers();

                foreach (var item in allLayers)
                {
                    var characterLabel = item.DefaultObject.name;

                    if (grayoutControllers.Contains(characterLabel)) { continue; }

                    characterGrayOutController.NoGrayoutCharacters.Add(characterLabel);
                    grayoutControllers.Add(characterLabel);
                }
            }
        }
    }
}
