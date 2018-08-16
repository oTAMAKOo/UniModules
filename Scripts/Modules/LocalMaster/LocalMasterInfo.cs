﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.LocalMaster
{
    public class LocalMasterInfo
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Type MasterAssetType { get; private set; }
        public string MasterName { get; private set; }
        public string SpreadsheetId { get; private set; }

        //----- method -----
        
        public LocalMasterInfo(string spreadsheetId, string masterName, Type masterAssetType)
        {
            this.SpreadsheetId = spreadsheetId;
            this.MasterName = masterName;
            
            if(masterAssetType.IsSubclassOf(typeof(LocalMasterAsset)))
            {
                this.MasterAssetType = masterAssetType;
            }
            else
            {
                var message = string.Format("LocalMasterAssetの継承型ではありません.\n{0}", masterAssetType.FullName);
                throw new ArgumentException(message);
            }            
        }
    }
}