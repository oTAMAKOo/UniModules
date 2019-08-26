
using UnityEngine;
using UnityEditor;
using System.Security.Cryptography;
using Extensions;

namespace Modules.Devkit.Memo
{
    [CustomEditor(typeof(Memo))]
    public sealed class MemoInspector : Editor
    {
        private const string AESKey = "Vk7DpPac9AMkRPLkP2nCdMxCKRHFzxtp";
        private const string AESIv = "4ieA3xRf88JfP9pN";

        private string text = null;
        private Vector2 scrollPosition = Vector2.zero;

        private static AesManaged aesManaged = null;

        void OnEnable()
        {
            if (aesManaged == null)
            {
                aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);
            }

            var instance = target as Memo;

            var memo = Reflection.GetPrivateField<Memo, string>(instance, "memo");

            if (!string.IsNullOrEmpty(memo))
            {
                text = memo.Decrypt(aesManaged);
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
                        Reflection.SetPrivateField(memo, "memo", text.Encrypt(aesManaged));
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }
    }
}
