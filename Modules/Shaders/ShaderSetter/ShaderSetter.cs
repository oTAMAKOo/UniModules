
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.Shaders
{
    [ExecuteAlways]
    public sealed class ShaderSetter : MonoBehaviour
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
        private Renderer rendererComponent = null;

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

            rendererComponent = UnityUtility.GetComponent<Renderer>(gameObject);

            if (rendererComponent != null)
            {
                type = Type.Renderer;
            }

            image = UnityUtility.GetComponent<Image>(gameObject);

            if (image != null)
            {
                type = Type.Image;
            }

            var material = GetMaterial();

            if (defaultShader == null && material != null)
            {
                defaultShader = material.shader;
            }

            // 適用処理は未初期化だと初期化を呼び出すため、初期化済みにしてから適用する.
            initialized = true;

            if (shader == null && !string.IsNullOrEmpty(shaderName))
            {
                var findShader = Shader.Find(shaderName);

                if (findShader != null)
                {
                    Set(findShader);
                }
                else
                {
                    Debug.LogError($"Shader not found. ({shaderName}).", this);
                }
            }
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

            if (shader == null || material == null){ return; }

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
                    material = Application.isPlaying ? rendererComponent.material : rendererComponent.sharedMaterial;
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
                        rendererComponent.material = material;
                    }
                    else
                    {
                        rendererComponent.sharedMaterial = material;
                    }
                    break;

                case Type.Image:
                    image.material = material;
                    break;
            }
        }
    }
}
