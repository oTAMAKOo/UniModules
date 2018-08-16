﻿﻿
using UnityEngine;
using Extensions;
using Extensions.Serialize;

namespace Modules.GameText.Components
{
    public class GameTextAsset : ScriptableObject
	{
        [SerializeField, ReadOnly]
        public LongNullable updateTime = null;
        [SerializeField, ReadOnly]
        public GameTextDictionary[] contents = new GameTextDictionary[0];
    }
}