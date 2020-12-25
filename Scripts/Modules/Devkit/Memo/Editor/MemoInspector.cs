
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

        private static AesCryptoKey aesCryptoKey = null;

        void OnEnable()
        {
            var config = MemoConfig.Instance;

            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey(config.AESKey, config.AESIv);
            }

            var instance = target as Memo;

            var memo = Reflection.GetPrivateField<Memo, string>(instance, "memo");

            if (!string.IsNullOrEmpty(memo))
            {
                text = memo.Decrypt(aesCryptoKey);
            }
        }

        public override void OnInspectorGUI()
        {
            var memo = target as Memo;

            var config = MemoConfig.Instance;

            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey(config.AESKey, config.AESIv);
            }

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
                        Reflection.SetPrivateField(memo, "memo", text.Encrypt(aesCryptoKey));
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }
    }
}
