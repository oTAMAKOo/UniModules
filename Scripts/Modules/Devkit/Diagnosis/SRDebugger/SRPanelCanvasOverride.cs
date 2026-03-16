
using UnityEngine;
using Cysharp.Threading.Tasks;

#if ENABLE_SRDEBUGGER

using SRF.UI;
using SRDebugger.Internal;
using SRDebugger.Services.Implementation;

#endif

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public abstract class SRPanelCanvasOverride
    {
        #if ENABLE_SRDEBUGGER

        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----
        
        public void Initialize()
        {
            SRDebug.Instance.PanelVisibilityChanged += OnPanelVisibilityChanged;

            // PinnedUI の Canvas 生成イベントを購読.
            Service.PinnedUI.OptionsCanvasCreated += OnPinnedUICanvasCreated;

            // PinnedUI等、既にロード済みのCanvasがあれば設定.
            ConfigureCanvasesAsync().Forget();
        }

        private void OnPinnedUICanvasCreated(RectTransform canvasRectTransform)
        {
            var canvas = canvasRectTransform.GetComponentInParent<Canvas>();

            if (canvas == null) { return; }

            ConfigureCanvas(canvas);
        }

        private void OnPanelVisibilityChanged(bool visible)
        {
            if (visible)
            {
                // DebugPanelは遅延ロードのため、表示時に設定.
                ConfigureCanvasesAsync().Forget();
            }
        }

        private async UniTaskVoid ConfigureCanvasesAsync()
        {
            // ConfigureCanvasFromSettings.Start() / SRRetinaScaler.Start() の完了を待つ.
            await UniTask.NextFrame();

            // DebugPanel の Canvas.
            var debugPanelService = Service.Panel as DebugPanelServiceImpl;

            if (debugPanelService != null && debugPanelService.IsLoaded)
            {
                var root = debugPanelService.RootObject;

                if (root != null && root.Canvas != null)
                {
                    ConfigureCanvas(root.Canvas);
                }
            }
        }

        protected abstract void ConfigureCanvas(Canvas canvas);
        
        #endif
    }
}
