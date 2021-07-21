
using UnityEngine;
using UnityEditor;
using Extensions;

namespace Modules.Devkit.Memo
{
    [CustomEditor(typeof(Memo))]
    public sealed class MemoInspector : Editor
    {
        private string text = null;
        private Vector2 scrollPosition = Vector2.zero;

        private static AesCryptoKey cryptoKey = null;

        void OnEnable()
        {
            var instance = target as Memo;

            var cryptoKey = GetCryptoKey();

            var memo = Reflection.GetPrivateField<Memo, string>(instance, "memo");

            if (!string.IsNullOrEmpty(memo))
            {
                text = memo.Decrypt(cryptoKey);
            }
        }

        public override void OnInspectorGUI()
        {
            var memo = target as Memo;

            EditorGUILayout.Separator();

            var style = GUI.skin.textField;
            var size = style.CalcSize(new GUIContent(text));
            var height = Mathf.Clamp(size.y, 80f, size.y);
            var scrollViewHeight = Mathf.Clamp(size.y, 85f, 200f);

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(scrollViewHeight)))
            {
                using (new GUILayout.VerticalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    text = EditorGUILayout.TextArea(text, GUILayout.Height(height), GUILayout.ExpandWidth(true));

                    if (EditorGUI.EndChangeCheck())
                    {
                        var cryptoKey = GetCryptoKey();

                        Reflection.SetPrivateField(memo, "memo", text.Encrypt(cryptoKey));
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        private AesCryptoKey GetCryptoKey()
        {
            var config = MemoConfig.Instance;

            if (cryptoKey == null)
            {
                cryptoKey = new AesCryptoKey(config.AESKey, config.AESIv);
            }

            return cryptoKey;
        }
    }
}
