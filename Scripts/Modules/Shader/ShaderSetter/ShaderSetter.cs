﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Shaders
{
    [ExecuteInEditMode]
	public class ShaderSetter : MonoBehaviour
	{
        //----- params -----

        private enum Type
        {
            None,
            Renderer,
            Image,
        }

        //----- field -----

        [SerializeField]
        private string shaderName = null;
        [SerializeField]
        private Shader defaultShader = null;

        private Shader shader = null;

        private Type type = Type.None;
        private Image image = null;
        private new Renderer renderer = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            if (initialized) { return; }

            type = Type.None;

            renderer = UnityUtility.GetComponent<Renderer>(gameObject);

            if (renderer != null)
            {
                type = Type.Renderer;
            }

            image = UnityUtility.GetComponent<Image>(gameObject);

            if (image != null)
            {
                type = Type.Image;
            }

            if (shader == null && !string.IsNullOrEmpty(shaderName))
            {
                Set(shaderName);
            }

            var material = GetMaterial();

            if (defaultShader == null && material != null)
            {
                defaultShader = material.shader;
            }

            initialized = true;
        }

        public void Set(string shaderName)
        {
            shader = string.IsNullOrEmpty(shaderName) ? null: Shader.Find(shaderName);

            Set(shader);
        }

        public void Set(Shader shader)
        {
            this.shader = shader;

            Apply();
        }

        public void Apply()
        {
            if(!initialized)
            {
                Setup();
            }

            var material = GetMaterial();

            shader = shader == null ? defaultShader : shader;

            shaderName = shader.name;

            var newMaterial = new Material(shader);

            newMaterial.mainTexture = material.mainTexture;
            newMaterial.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            SetMaterial(newMaterial);
        }

        private Material GetMaterial()
        {
            Material material = null;

            switch (type)
            {
                case Type.Renderer:
                    material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
                    break;

                case Type.Image:
                    material = image.material;
                    break;
            }

            return material;
        }

        private void SetMaterial(Material material)
        {
            switch (type)
            {
                case Type.Renderer:
                    if (Application.isPlaying)
                    {
                        renderer.material = material;
                    }
                    else
                    {
                        renderer.sharedMaterial = material;
                    }
                    break;

                case Type.Image:
                    image.material = material;
                    break;
            }
        }
    }
}