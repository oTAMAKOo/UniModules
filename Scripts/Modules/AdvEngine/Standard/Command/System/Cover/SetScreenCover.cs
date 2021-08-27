
#if ENABLE_MOONSHARP

using UnityEngine.UI;
using System;
using Extensions;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class SetScreenCover : Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public override string CommandName { get { return "SetCover"; } }

        public Image FadeImage { get; set; }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<bool, string>)CommandFunction;
        }

        private void CommandFunction(bool show, string colorCode = "#000000FF")
        {
            try
            {
                var color = colorCode.HexToColor();

                FadeImage.color = color;

                UnityUtility.SetActive(FadeImage, show);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
