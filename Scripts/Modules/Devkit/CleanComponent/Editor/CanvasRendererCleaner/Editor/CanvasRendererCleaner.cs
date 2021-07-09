
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using System.Linq;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.CleanComponent
{
    public abstract class CanvasRendererCleaner
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoClean
            {
                get { return ProjectPrefs.GetBool("CanvasRendererCleanerPrefs-autoClean", true); }
                set { ProjectPrefs.SetBool("CanvasRendererCleanerPrefs-autoClean", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        protected static void ModifyComponent(GameObject rootObject)
        {
            var canvasRenderers = rootObject.DescendantsAndSelf().OfComponent<CanvasRenderer>();

            foreach (var canvasRenderer in canvasRenderers)
            {
                var gameObject = canvasRenderer.gameObject;

                var graphicComponents = gameObject.GetComponents<Graphic>();

                if (graphicComponents.Any()) { continue; }

                UnityUtility.DeleteGameObject(canvasRenderer);

                EditorUtility.SetDirty(gameObject);
            }
        }

        protected static bool CheckExecute(GameObject[] gameObjects)
        {
            var modify = gameObjects.SelectMany(x => x.DescendantsAndSelf().OfComponent<CanvasRenderer>())
                .Any(x => x.GetComponents<Graphic>().IsEmpty());

            if (modify)
            {
                return EditorUtility.DisplayDialog("CanvasRenderer Cleaner", "CanvasRenderer has not graphic component.\nDo you want to run cleanup?", "Clean", "Keep");
            }

            return false;
        }
    }
}
