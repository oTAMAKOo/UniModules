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
    public partial class GameText : Singleton<GameText>
    {
        //----- params -----

        public const string GameTextAsset = "Resources/GameText.asset";

        //----- field -----

        private Dictionary<int, GameTextDictionary> cache = new Dictionary<int, GameTextDictionary>();

        //----- property -----

        public Dictionary<int, GameTextDictionary> Cache { get { return cache; } }

        //----- method -----

        private GameText() { }

        public void Load()
        {
            var asset = Resources.Load<GameTextAsset>(UnityPathUtility.ConvertResourcesLoadPath(GameTextAsset));

            if (asset != null)
            {
                cache = asset.contents.ToDictionary(x => x.SheetId);
            }
        }

        // ※ SheetIdがGameTextCategoryのEnumIDとして登録されている.

        public static string Get(GameTextCategory category, int enumValue)
        {
            if (Instance == null) { return null; }

            if (category != GameTextCategory.None)
            {
                var texts = Instance.cache.GetValueOrDefault((int)category);

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
                var texts = Instance.cache.GetValueOrDefault((int)category);

                return texts != null ? texts.GetValueOrDefault(int.Parse(id.ToString("d")), null) : null;
            }

            return null;
        }
    }
}