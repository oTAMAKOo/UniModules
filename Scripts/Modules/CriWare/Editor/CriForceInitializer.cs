
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using CriWare;
using Extensions;

namespace Modules.CriWare.Editor
{
    public static class CriForceInitializer
    {
        public static void Initialize()
        {
            var initializerCreate = false;

            CriWareInitializer initializer = null;

            if (!CriWareInitializer.IsInitialized())
            {
                initializer = UnityUtility.FindObjectOfType<CriWareInitializer>();

                if (initializer == null)
                {
                    initializer = UnityUtility.CreateGameObject<CriWareInitializer>(null, "CriWareInitializer");
                    initializer.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                    initializerCreate = true;
                }

                initializer.Initialize();
            }

            if (initializer != null && initializerCreate)
            {
                UnityUtility.SafeDelete(initializer.gameObject);
            }
        }
    }
}

#endif
