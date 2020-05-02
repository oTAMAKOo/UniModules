﻿﻿
using UnityEngine;
using Extensions;
using Extensions.Serialize;

namespace Modules.GameText.Components
{
    public sealed class GameTextAsset : ScriptableObject
	{
        [SerializeField, ReadOnly]
        public LongNullable updateTime = null;
        [SerializeField, ReadOnly]
        public GameText.GameTextDictionary[] contents = new GameText.GameTextDictionary[0];
    }
}
