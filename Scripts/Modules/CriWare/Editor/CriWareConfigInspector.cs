
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using UnityEditor;
using System;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;

namespace Modules.CriWare
{
    [CustomEditor(typeof(CriWareConfig), true)]
    public sealed class CriWareConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        private enum InitializeState
        {
            None,
            Running,
            Done,
        }

        //----- field -----

        private CriWareConfig instance = null;

        private AesCryptoKey cryptoKey = null;

        private string criwareKey = null;

        [NonSerialized]
        private InitializeState initializeState = InitializeState.Done;

        //----- property -----

        //----- method -----

        private async UniTask Initialize()
        {
            if (initializeState != InitializeState.None) { return; }

            initializeState = InitializeState.Running;

            cryptoKey = await instance.GetCryptoKey();

            criwareKey = await instance.GetCriWareKey();

            initializeState = InitializeState.Done;
        }

        void OnEnable()
        {
            initializeState = InitializeState.None;
        }

        public override void OnInspectorGUI()
        {
            instance = target as CriWareConfig;

            if (initializeState == InitializeState.None)
            {
                Initialize().Forget();
            }

            if (initializeState != InitializeState.Done) { return; }

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            criwareKey = EditorGUILayout.DelayedTextField("CriWare Key", criwareKey);

            if (EditorGUI.EndChangeCheck())
            {
                var encryptKey = criwareKey.Encrypt(cryptoKey);

                Reflection.SetPrivateField(instance, "key", encryptKey);

                UnityEditorUtility.SaveAsset(instance);
            }
        }
    }
}

#endif