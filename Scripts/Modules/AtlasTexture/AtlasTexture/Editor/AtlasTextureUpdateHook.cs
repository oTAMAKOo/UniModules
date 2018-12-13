
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Unity.Linq;

namespace Modules.Atlas
{
    public static class AtlasTextureUpdateHook
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        public static void InitializeOnLoadMethod()
        {
            PrefabUtility.prefabInstanceUpdated += ApplyAtlasTextureImage;
        }

        private static void ApplyAtlasTextureImage(GameObject instance)
        {
            var atlasTextureImages = instance.DescendantsAndSelf().OfComponent<AtlasTextureImage>();

            foreach (var atlasTextureImage in atlasTextureImages)
            {
                if (atlasTextureImage.Atlas != null)
                {
                    atlasTextureImage.Apply();
                }
            }
        }
    }
}
