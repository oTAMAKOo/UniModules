﻿
using UnityEngine;
using System.Collections;

namespace Modules.ExternalResource
{
    public class ShaderReApply : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Renderer[] renderers;
        private Material[] materials;
        private string[] shaders;

        //----- property -----

        //----- method -----

        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                materials = renderer.sharedMaterials;
                shaders = new string[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    shaders[i] = materials[i].shader.name;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].shader = Shader.Find(shaders[i]);
                }
            }
        }
    }
}