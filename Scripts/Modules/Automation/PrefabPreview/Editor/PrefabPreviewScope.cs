
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System;
using Extensions;
using Modules.TextData.Editor;

namespace Modules.Automation.PrefabPreview
{
    /// <summary> Prefabを隔離したプレビューシーンに展開するスコープ. アセット・開いているシーン・PrefabStageには一切触れない </summary>
    public sealed class PrefabPreviewScope : Scope
    {
        //----- params -----

        private const string CameraObjectName = "PrefabPreviewCamera";

        private const string CanvasObjectName = "PrefabPreviewCanvas";

        private const float CameraDistance = 100f;

        //----- field -----

        private UnityEngine.SceneManagement.Scene scene = default;

        private Camera camera = null;

        private Canvas canvas = null;

        private GameObject instance = null;

        private PrefabPreviewOptions options = null;

        //----- property -----

        public UnityEngine.SceneManagement.Scene Scene { get { return scene; } }

        public Camera Camera { get { return camera; } }

        public Canvas Canvas { get { return canvas; } }

        /// <summary> 展開したPrefabインスタンス(キャンバス直下) </summary>
        public GameObject Instance { get { return instance; } }

        //----- method -----

        public PrefabPreviewScope(GameObject prefab, PrefabPreviewOptions options = null)
        {
            if (prefab == null) { throw new ArgumentNullException(nameof(prefab)); }

            if (options == null) { options = new PrefabPreviewOptions(); }

            this.options = options;

            scene = EditorSceneManager.NewPreviewScene();

            try
            {
                PrepareTextData();

                camera = CreateCamera();
                canvas = CreateCanvas();
                instance = Instantiate(prefab);

                RebuildLayout();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary> レイアウトを強制再計算する </summary>
        public void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();

            var rectTransform = instance.transform as RectTransform;

            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary> 現在の状態を描画してTexture2Dを返す(呼び出し側で破棄する) </summary>
        public Texture2D Render()
        {
            var width = options.Width;
            var height = options.Height;

            var previousActive = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;

                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        /// <summary> 未ロードならTextDataを読み込む(展開したインスタンスの文言解決に必要) </summary>
        private static void PrepareTextData()
        {
            if (TextDataLoader.IsLoaded) { return; }

            TextDataLoader.Reload();
        }

        private Camera CreateCamera()
        {
            var gameObject = EditorUtility.CreateGameObjectWithHideFlags(CameraObjectName, HideFlags.HideAndDontSave, typeof(Camera));

            SceneManager.MoveGameObjectToScene(gameObject, scene);

            gameObject.transform.position = new Vector3(0f, 0f, -CameraDistance);

            var previewCamera = gameObject.GetComponent<Camera>();

            // 手動Render専用. エディタの通常描画には参加させない.
            // CameraType.Previewではキャンバスの更新内容が描画へ反映されないためGameとして描画する.
            previewCamera.enabled = false;
            previewCamera.cameraType = CameraType.Game;
            previewCamera.scene = scene;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = options.Height * 0.5f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = CameraDistance * 2f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = options.BackgroundColor;
            previewCamera.cullingMask = -1;
            previewCamera.useOcclusionCulling = false;
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;

            return previewCamera;
        }

        private Canvas CreateCanvas()
        {
            var gameObject = EditorUtility.CreateGameObjectWithHideFlags(CanvasObjectName, HideFlags.HideAndDontSave, typeof(RectTransform), typeof(Canvas));

            SceneManager.MoveGameObjectToScene(gameObject, scene);

            // WorldSpaceキャンバスを解像度と同じサイズで原点に置き、正射影カメラで1px=1unitに写す.
            var rectTransform = gameObject.transform as RectTransform;

            rectTransform.position = Vector3.zero;
            rectTransform.sizeDelta = new Vector2(options.Width, options.Height);

            var previewCanvas = gameObject.GetComponent<Canvas>();

            previewCanvas.renderMode = RenderMode.WorldSpace;
            previewCanvas.worldCamera = camera;

            return previewCanvas;
        }

        private GameObject Instantiate(GameObject prefab)
        {
            var gameObject = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;

            if (gameObject == null)
            {
                // プレビューシーンへ直接展開できない場合は通常生成して移動する.
                gameObject = UnityUtility.Instantiate(null, prefab);
                gameObject.name = prefab.name;

                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }

            UnityUtility.SetParent(gameObject, canvas.gameObject, false);

            if (options.OnInstantiated != null)
            {
                options.OnInstantiated(gameObject);
            }

            return gameObject;
        }

        protected override void CloseScope()
        {
            // ファイナライザ経由(別スレッド)ではUnity APIを呼べない.
            if (!InternalEditorUtility.CurrentThreadIsMainThread()) { return; }

            if (scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            scene = default;
            camera = null;
            canvas = null;
            instance = null;
        }
    }
}
