
using UnityEngine;
using Modules.Devkit.Prefs;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public sealed class TextureConfig : ReloadableScriptableObject<TextureConfig>
    {
        //----- params -----

        public static class Prefs
        {
            public static bool changeSettingOnImport
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-changeSettingOnImport", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-changeSettingOnImport", value); }
            }
        }

        //----- field -----

        // compress

        [SerializeField]
        private Object[] compressFolders = null;
        [SerializeField]
        private string[] ignoreCompressFolders = null;

        // sprite

        [SerializeField]
        private Object[] spriteFolders = null;
        [SerializeField]
        private string[] spriteFolderNames = null;
        [SerializeField]
        private string[] ignoreSpriteFolders = null;
        
        //----- property -----

        /// <summary> 圧縮設定を適用するフォルダ. </summary>
        public Object[] CompressFolders
        {
            get { return compressFolders ?? (compressFolders = new Object[0]); }
        }

        /// <summary> 圧縮設定の適用から除外するフォルダ名. </summary>
        public string[] IgnoreCompressFolders
        {
            get { return ignoreCompressFolders ?? (ignoreCompressFolders = new string[0]); }
        }

        /// <summary> TextureTypeをSpriteに設定するフォルダ. </summary>
        public Object[] SpriteFolders
        {
            get { return spriteFolders ?? (spriteFolders = new Object[0]); }
        }

        /// <summary> TextureTypeをSpriteに設定するフォルダ名. </summary>
        public string[] SpriteFolderNames
        {
            get { return spriteFolderNames ?? (spriteFolderNames = new string[0]); }
        }

        /// <summary> TextureTypeをSpriteに設定適用から除外するフォルダ名. </summary>
        public string[] IgnoreSpriteFolders
        {
            get { return ignoreSpriteFolders ?? (ignoreSpriteFolders = new string[0]); }
        }

        //----- method -----
    }
}
