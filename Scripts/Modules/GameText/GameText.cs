﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.GameText.Components;

namespace Modules.GameText
{
    public sealed partial class GameText : Singleton<GameText>
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public Dictionary<int, GameTextDictionary> Cache { get; private set; }

        //----- method -----

        private GameText()
        {
            Cache = null;
        }

        public void Load(string assetPath)
        {
            Cache = null;

            var asset = Resources.Load<GameTextAsset>(UnityPathUtility.ConvertResourcesLoadPath(assetPath));

            if (asset != null)
            {
                Cache = asset.contents.ToDictionary(x => x.SheetId);
            }
        }

        // ※ SheetIdがGameTextCategoryのEnumIDとして登録されている.

        public string Find(GameTextCategory category, int enumValue)
        {
            if (Instance == null) { return null; }

            if (Instance.Cache == null) { return null; }

            if (category != GameTextCategory.None)
            {
                var texts = Instance.Cache.GetValueOrDefault((int)category);

                return texts != null ? texts.GetValueOrDefault(enumValue, null) : null;
            }

            return null;
        }

        public static string Get(Enum id)
        {
            if(Instance == null) { return null; }

            var category = Instance.CategoryTable.GetValueOrDefault(id.GetType(), GameTextCategory.None);

            if (category != GameTextCategory.None)
            {
                var texts = Instance.Cache.GetValueOrDefault((int)category);

                return texts != null ? texts.GetValueOrDefault(int.Parse(id.ToString("d")), null) : null;
            }

            return null;
        }
    }
}
