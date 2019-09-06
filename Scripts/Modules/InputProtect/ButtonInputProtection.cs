
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Unity.Linq;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Modules.InputProtection.Components
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonInputProtection : InputProtection
    {
        //----- params -----

        //----- field -----

        private Button target = null;

        private List<Graphic> raycastGraphics = null;

        //----- property -----

        //----- method -----

        protected override void UpdateProtect(bool isProtect)
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
                if (isProtect)
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

        #if UNITY_EDITOR

        [CustomEditor(typeof(ButtonInputProtection))]
        class ButtonInputProtectionInspector : Editor
        {
            public override void OnInspectorGUI(){}
        }

        #endif
    }
}
