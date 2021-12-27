
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.InputControl.Components
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonInputBlockListener : InputBlockListener
    {
        //----- params -----

        //----- field -----

        private Button target = null;

        private List<Graphic> raycastGraphics = null;

        //----- property -----

        //----- method -----

        protected override void UpdateInputBlock(bool isBlock)
        {
            if (raycastGraphics == null)
            {
                raycastGraphics = new List<Graphic>();
            }

            if (target == null)
            {
                target = UnityUtility.GetComponent<Button>(gameObject);
            }

            // interactiveの状態でTintColorが変わってしまうのを防ぐ.
            if (target.targetGraphic != null)
            {
                if (isBlock)
                {
                    var graphics = target.targetGraphic.gameObject
                        .DescendantsAndSelf()
                        .OfComponent<Graphic>()
                        .ToArray();

                    raycastGraphics.Clear();

                    foreach (var graphic in graphics)
                    {
                        if (graphic.raycastTarget)
                        {
                            raycastGraphics.Add(graphic);
                        }

                        graphic.raycastTarget = false;
                    }
                }
                else
                {
                    foreach (var raycastGraphic in raycastGraphics)
                    {
                        raycastGraphic.raycastTarget = true;
                    }
                }
            }
        }
    }
}
