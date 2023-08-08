
#if UNITY_EDITOR

using UnityEngine;
using System.Linq;
using System.Reflection;
using Unity.Linq;
using Extensions;
using Modules.TextData.Components;
using Modules.UI.DummyContent;

namespace Modules.Devkit.CleanComponent
{
    public static class DummyTextCleaner
    {
        //----- params -----

        //----- field -----

        private static MethodInfo dummyTextApplyMethodInfo = null;
        private static MethodInfo dummyTextCleanMethodInfo = null;
    
        private static MethodInfo textSetterApplyMethodInfo = null;
        private static MethodInfo textSetterCleanMethodInfo = null;

        //----- property -----
        
        //----- method -----

        public static bool ModifyComponents(GameObject rootObject)
        {
            if (dummyTextCleanMethodInfo == null)
            {
                dummyTextCleanMethodInfo = Reflection.GetMethodInfo(typeof(DummyText), "CleanDummyText", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (textSetterCleanMethodInfo == null)
            {
                textSetterCleanMethodInfo = Reflection.GetMethodInfo(typeof(TextSetter), "CleanDummyText", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var changed = false;

            var components = rootObject.DescendantsAndSelf()
                .Select(x => UnityUtility.GetComponents<Component>(x))
                .SelectMany(x => x);

            foreach (var component in components)
            {
                switch (component)
                {
                    case DummyText dummyText:
                        changed |= (bool)dummyTextCleanMethodInfo.Invoke(dummyText, null);
                        break;
                    case TextSetter textSetter:
                        changed |= (bool)textSetterCleanMethodInfo.Invoke(textSetter, null);
                        break;
                }
            }

            return changed;
        }

        public static void ReApply(GameObject rootObject)
        {
            if (dummyTextApplyMethodInfo == null)
            {
                dummyTextApplyMethodInfo = Reflection.GetMethodInfo(typeof(DummyText), "ApplyDummyText", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (textSetterApplyMethodInfo == null)
            {
                textSetterApplyMethodInfo = Reflection.GetMethodInfo(typeof(TextSetter), "ApplyDummyText", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var components = rootObject.DescendantsAndSelf()
                .Select(x => UnityUtility.GetComponents<Component>(x))
                .SelectMany(x => x);

            foreach (var component in components)
            {
                switch (component)
                {
                    case DummyText dummyText:
                        dummyTextApplyMethodInfo.Invoke(dummyText, null);
                        break;
                    case TextSetter textSetter:
                        textSetterApplyMethodInfo.Invoke(textSetter, null);
                        break;
                }
            }
        }
    }
}

#endif
