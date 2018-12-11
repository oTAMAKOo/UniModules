
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Utage;
using Extensions;

namespace Modules.UtageExtension
{
	public class AdvExtendCommandEmotionOff : AdvCommand
    {
        //----- params -----

        //----- field -----

        private string name = null;

        //----- property -----

        //----- method -----

        public AdvExtendCommandEmotionOff(StringGridRow row) : base(row)
		{
            name = ParseCellOptional<string>(AdvColumnName.Arg1, "");
        }

        public override void DoCommand(AdvEngine engine)
        {
            if (string.IsNullOrEmpty(name))
            {
                engine.GraphicManager.SpriteManager.FadeOutAll(0f);
            }
            else
            {
                engine.GraphicManager.SpriteManager.FadeOut(name, 0f);
            }
        }
    }
}

#endif
