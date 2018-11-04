﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.CleanComponent
{
    public interface IComponentCleaner
    {
        void Clean(GameObject[] allObjects);
    }

    public static class ComponentCleaner
	{
        //----- params -----

        private static readonly IComponentCleaner[] CleanTargets = new IComponentCleaner[]
        {
            new TextComponentCleaner(),
            new ImageComponentCleaner(),
        };

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Execute()
        {
            var allObjects = UnityEditorUtility.FindAllObjectsInHierarchy(false);

            foreach (var target in CleanTargets)
            {
                target.Clean(allObjects);
            }
            
            InternalEditorUtility.RepaintAllViews();

            Debug.Log("Finish clean component.");
        }
	}
}
