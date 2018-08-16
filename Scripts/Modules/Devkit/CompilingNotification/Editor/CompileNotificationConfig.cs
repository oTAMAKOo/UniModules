
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.CompileNotice
{
    public class CompileNotificationConfig : ReloadableScriptableObject<CompileNotificationConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Texture[] animationTextures = null;

        //----- property -----

        public Texture[] AnimationTextures { get { return animationTextures; } }

        //----- method -----
    }
}