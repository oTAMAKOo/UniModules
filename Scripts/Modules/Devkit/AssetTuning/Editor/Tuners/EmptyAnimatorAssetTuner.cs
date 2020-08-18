
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using Extensions.Devkit;
using Modules.Devkit.AssetTuning;

namespace Modules.Devkit.AssetTuning
{
    public sealed class EmptyAnimatorAssetTuner : AssetTuner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            AssetTuneManager.Instance.Register<EmptyAnimatorAssetTuner>();
        }

        public override bool Validate(string path)
        {
            if (Path.GetExtension(path) != ".controller") { return false; }

            var asset = AssetDatabase.LoadMainAssetAtPath(path);

            var controller = asset as AnimatorController;

            if (controller == null) { return false; }

            if (controller.parameters.Any()) { return false; }

            foreach (var layer in controller.layers)
            {
                var stateMachine = layer.stateMachine;

                if (stateMachine.states.Any()) { return false; }                
                if (stateMachine.anyStateTransitions.Any()) { return false; }
                if (stateMachine.entryTransitions.Any()) { return false; }                
                if (stateMachine.stateMachines.Any()) { return false; }
                if (stateMachine.behaviours.Any()) { return false; }
            }

            return true;
        }

        public override void OnAssetCreate(string path)
        {
            RegisterTuneAssetCallback(path);
        }

        private void RegisterTuneAssetCallback(string path)
        {
            // ※ Importされた時点ではAnimatorの初期化が終わっていないのでImport完了後に処理を実行する.

            EditorApplication.CallbackFunction tuningCallbackFunction = null;

            tuningCallbackFunction = () =>
            {
                TuneAsset(path);

                if (tuningCallbackFunction != null)
                {
                    EditorApplication.delayCall -= tuningCallbackFunction;
                }
            };

            EditorApplication.delayCall += tuningCallbackFunction;
        }

        private static void TuneAsset(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);

            var controller = asset as AnimatorController;

            if (controller == null) { return; }

            var rootStateMachine = controller.layers[0].stateMachine;

            // Add DefaultState.

            var defaultState = rootStateMachine.AddState("Default");

            var state = rootStateMachine.states.FirstOrDefault(x => x.state == defaultState);

            state.position = new Vector2(50f, 50f);

            // Set EntryTransition.

            rootStateMachine.AddEntryTransition(defaultState);

            // Set Position.

            rootStateMachine.entryPosition = new Vector2(0f, 0f);
            rootStateMachine.anyStatePosition = new Vector2(0f, 50f);
            rootStateMachine.exitPosition = new Vector2(0f, 100f);

            UnityEditorUtility.SaveAsset(controller);
        }
    }
}
