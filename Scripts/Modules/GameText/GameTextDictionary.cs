﻿﻿
using UnityEngine;
using System;
using Extensions.Serialize;
using Extensions;

namespace Modules.GameText.Components
{
    [Serializable]
    public sealed class GameTextDictionary : SerializableDictionary<int, string>
    {
        [SerializeField, ReadOnly]
        private string sheetName = string.Empty;
        [SerializeField, ReadOnly]
        private IntNullable sheetId = new IntNullable(0);

        public string SheetName
        {
            get { return sheetName; }
            set { sheetName = value; }
        }

        public int SheetId
        {
            get { return sheetId.Value; }
            set { sheetId.Value = value; }
        }
    }
}
