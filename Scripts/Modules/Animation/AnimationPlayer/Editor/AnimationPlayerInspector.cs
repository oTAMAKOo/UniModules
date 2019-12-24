
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

using AnimatorController = UnityEditor.Animations.AnimatorController;

namespace Modules.Animation
{
    [CustomEditor(typeof(AnimationPlayer))]
    public class AnimationPlayerInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private Animator animator = null;

        private AnimationPlayer instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as AnimationPlayer;

            animator = UnityUtility.GetComponent<Animator>(instance.gameObject);

            if (!instance.IsInitialized)
            {
                Reflection.InvokePrivateMethod(instance, "Initialize");
            }
        }

        public override void OnInspectorGUI()
        {
            instance = target as AnimationPlayer;

            if (animator == null) { return; }

            var animatorController = animator.runtimeAnimatorController;

            var stopOnAwake = Reflection.GetPrivateField<AnimationPlayer, bool>(instance, "stopOnAwake");
            var ignoreTimeScale = Reflection.GetPrivateField<AnimationPlayer, bool>(instance, "ignoreTimeScale");
            var endActionType = Reflection.GetPrivateField<AnimationPlayer, EndActionType>(instance, "endActionType");

            if (animatorController != null)
            {
                GUILayout.Space(8f);

                EditorGUI.BeginChangeCheck();

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(150f);

                stopOnAwake = EditorGUILayout.Toggle("Stop On Awake", stopOnAwake);

                ignoreTimeScale = EditorGUILayout.Toggle("Ignore TimeScale", ignoreTimeScale);

                endActionType = (EndActionType)EditorGUILayout.EnumPopup("End Action", endActionType);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("AnimationPlayerInspector Undo", instance);

                    Reflection.SetPrivateField(instance, "stopOnAwake", stopOnAwake);
                    Reflection.SetPrivateField(instance, "ignoreTimeScale", ignoreTimeScale);
                    Reflection.SetPrivateField(instance, "endActionType", endActionType);
                }

                EditorLayoutTools.SetLabelWidth(originLabelWidth);
            }
            else
            {
                EditorGUILayout.HelpBox("Please assign AnimationController asset.", MessageType.Info);
            }
        }
    }
}
