
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
	public abstract class ExtendCustomCommandManager : AdvCustomCommandManager
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnBootInit()
        {
            AdvCommandParser.OnCreateCustomCommandFromID += CreateCustomCommand;
        }

        public abstract void CreateCustomCommand(string id, StringGridRow row, AdvSettingDataManager dataManager, ref AdvCommand command);
    }
}