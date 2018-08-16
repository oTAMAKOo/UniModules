
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

            animator = UnityUtility.GetOrAddComponent<Animator>(instance.gameObject);

            if (!instance.IsInitialized)
            {
                Reflection.InvokePrivateMethod(instance, "Initialize");
            }
        }

        public override void OnInspectorGUI()
        {
            instance = target as AnimationPlayer;

            var animatorController = Reflection.GetPrivateField<AnimationPlayer, RuntimeAnimatorController>(instance, "animatorController") as AnimatorController;
            var endActionType = Reflection.GetPrivateField<AnimationPlayer, EndActionType>(instance, "endActionType");
            var ignoreTimeScale = Reflection.GetPrivateField<AnimationPlayer, bool>(instance, "ignoreTimeScale");

            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            animatorController = EditorGUILayout.ObjectField("Controller", animatorController, typeof(AnimatorController), false) as AnimatorController;

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("AnimationPlayerInspector Undo", instance);

                Reflection.SetPrivateField(instance, "animatorController", animatorController);
            }
            
            if (animator != null && animatorController != null)
            {
                EditorGUILayout.Separator();

                if (EditorLayoutTools.DrawHeader("Option", "AnimationPlayerInspector-Option"))
                {
                    using (new ContentsScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        var originLabelWidth = EditorLayoutTools.SetLabelWidth(150f);

                        endActionType = (EndActionType)EditorGUILayout.EnumPopup("End Action", endActionType);
                        ignoreTimeScale = EditorGUILayout.Toggle("Ignore TimeScale", ignoreTimeScale);

                        if (EditorGUI.EndChangeCheck())
                        {
                            UnityEditorUtility.RegisterUndo("AnimationPlayerInspector Undo", instance);
                            
                            Reflection.SetPrivateField(instance, "endActionType", endActionType);
                            Reflection.SetPrivateField(instance, "ignoreTimeScale", ignoreTimeScale);
                        }

                        EditorLayoutTools.SetLabelWidth(originLabelWidth);
                    }
                }

                EditorGUILayout.Separator();
            }
        }

        /// <summary>
        /// レイヤー名からレイヤーインデックスに変換.
        /// </summary>
        public int ConvertLayerNameToIndex(AnimatorController animatorController, string layerName)
        {
            if (string.IsNullOrEmpty(layerName)) { return -1; }

            var layers = animatorController.layers;

            return layers.IndexOf(x => x.name == layerName);
        }
    }
}