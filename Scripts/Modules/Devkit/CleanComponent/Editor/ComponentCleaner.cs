﻿
using UnityEngine;
using UnityEditorInternal;

namespace Modules.Devkit.CleanComponent
{
    public static class ComponentCleaner
	{
        public static void Execute()
        {
            SceneParticleComponentCleaner.Clean();

            InternalEditorUtility.RepaintAllViews();

            Debug.Log("Finish clean component.");
        }
	}
}
