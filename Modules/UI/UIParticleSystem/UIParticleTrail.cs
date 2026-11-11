
using UnityEngine;
using UnityEngine.UI;
using System;
using Extensions;

namespace Modules.UI.Particle
{
    /// <summary>
    /// UIParticleSystemのTrailをuGUI上に描画する内部コンポーネント.
    /// UIParticleSystemがTrailModule有効時に隠し子として自動生成するため、直接アタッチ・設定は行わない.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIParticleTrail : MaskableGraphic
    {
        //----- params -----

        private const int MaxVertexCount = 65535;

        //----- field -----

        private ParticleSystem particleSystem = null;

        private ParticleSystemRenderer particleSystemRenderer = null;

        private Camera bakeCamera = null;

        private Mesh bakedMesh = null;

        private Mesh workerMesh = null;

        private CombineInstance[] combineInstances = null;

        private Material currentTrailMaterial = null;

        /// <summary> 描画を空にしたか (空のあいだは空のメッシュを入れ直さない). </summary>
        [NonSerialized]
        private bool cleared = false;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        /// <summary> Trailマテリアル自身のテクスチャを使用するためテクスチャの上書きは行わない. </summary>
        public override Texture mainTexture { get { return null; } }

        public override Material material
        {
            get
            {
                if (particleSystemRenderer != null)
                {
                    var trailMaterial = particleSystemRenderer.trailMaterial;

                    if (trailMaterial != null){ return trailMaterial; }
                }

                return base.material;
            }

            set { base.material = value; }
        }

        //----- method -----

        /// <summary> 参照設定 (冪等・リロード後の残留物再利用時にも呼び直される). </summary>
        public void Setup(ParticleSystem particleSystem, ParticleSystemRenderer particleSystemRenderer)
        {
            this.particleSystem = particleSystem;
            this.particleSystemRenderer = particleSystemRenderer;

            Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // リロード・再アクティブ化で残った前回のメッシュ表示を消す (次のUpdateTrailで再設定される).
            canvasRenderer.Clear();

            cleared = true;
        }

        private void Initialize()
        {
            if (initialized){ return; }

            raycastTarget = false;

            bakedMesh = new Mesh()
            {
                name = "UIParticleTrail(Baked)",
                hideFlags = HideFlags.HideAndDontSave,
            };

            workerMesh = new Mesh()
            {
                name = "UIParticleTrail(Worker)",
                hideFlags = HideFlags.HideAndDontSave,
            };

            combineInstances = new CombineInstance[1];

            initialized = true;
        }

        /// <summary>
        /// Trailメッシュの更新を予約 (UIParticleSystemから毎フレーム駆動される).
        /// ベイクはキャンバスの更新 (粒の計算のあと) で行う.
        /// </summary>
        public void UpdateTrail(Camera bakeCamera, bool maskable)
        {
            if (!initialized){ return; }

            this.bakeCamera = bakeCamera;
            this.maskable = maskable;

            SetVerticesDirty();
        }

        public override void Rebuild(CanvasUpdate update)
        {
            // ※ 切り捨て中も描く範囲を求め直すため、ベイクは切り捨てに関係なく行う.
            if (update == CanvasUpdate.PreRender)
            {
                BakeTrail();
            }

            base.Rebuild(update);
        }

        private void BakeTrail()
        {
            if (!initialized){ return; }

            if (particleSystem == null){ return; }
            if (particleSystemRenderer == null){ return; }

            // ベイク用カメラがない場合 (Screen Space - Overlay等) は描画不可.
            if (bakeCamera == null)
            {
                Clear();
                return;
            }

            // 粒も軌跡も無いときはベイクしない (粒が消えたあとも残りの軌跡は描く).
            if (particleSystem.particleCount <= 0 && cleared){ return; }

            bakedMesh.Clear(false);

            // レンダラー無効状態ではベイクできないため一時的に有効化する.
            var rendererEnabled = particleSystemRenderer.enabled;

            particleSystemRenderer.enabled = true;

            particleSystemRenderer.BakeTrailsMesh(bakedMesh, bakeCamera, ParticleSystemBakeMeshOptions.BakeRotationAndScale);

            particleSystemRenderer.enabled = rendererEnabled;

            if (bakedMesh.vertexCount <= 0)
            {
                Clear();
                return;
            }

            if (MaxVertexCount <= bakedMesh.vertexCount)
            {
                Debug.LogError($"Too many trail vertices to render. ({bakedMesh.vertexCount} >= {MaxVertexCount})");

                Clear();
                return;
            }

            // ベイク結果 (ワールド空間) を自身のローカル空間へ変換.
            combineInstances[0].mesh = bakedMesh;
            combineInstances[0].transform = canvasRenderer.transform.worldToLocalMatrix * GetBakeCorrectionMatrix();

            workerMesh.Clear(false);
            workerMesh.CombineMeshes(combineInstances, true, true);

            canvasRenderer.SetMesh(workerMesh);

            cleared = false;

            // trailMaterialの差し替えに追従.
            var trailMaterial = particleSystemRenderer.trailMaterial;

            if (currentTrailMaterial != trailMaterial)
            {
                currentTrailMaterial = trailMaterial;

                SetMaterialDirty();
            }
        }

        /// <summary> ベイク結果 (回転・スケール適用済み・位置なし) に対する位置補正行列を取得. </summary>
        private Matrix4x4 GetBakeCorrectionMatrix()
        {
            var mainModule = particleSystem.main;

            var simulationSpace = mainModule.simulationSpace;

            // Trailがワールド空間モードの場合、頂点はワールド座標で記録されている.
            if (particleSystem.trails.worldSpace)
            {
                simulationSpace = ParticleSystemSimulationSpace.World;
            }

            switch (simulationSpace)
            {
                case ParticleSystemSimulationSpace.Local:
                    return Matrix4x4.Translate(particleSystem.transform.position);

                case ParticleSystemSimulationSpace.World:
                    return Matrix4x4.identity;

                case ParticleSystemSimulationSpace.Custom:
                    {
                        var customSpace = mainModule.customSimulationSpace;

                        // カスタム空間未設定時はローカル扱い (Unityの挙動に準拠).
                        if (customSpace == null)
                        {
                            return Matrix4x4.Translate(particleSystem.transform.position);
                        }

                        return Matrix4x4.Translate(customSpace.position);
                    }
            }

            return Matrix4x4.identity;
        }

        /// <summary> Trailの描画をクリア. </summary>
        public void Clear()
        {
            if (!initialized){ return; }

            if (cleared){ return; }

            workerMesh.Clear(false);

            canvasRenderer.SetMesh(workerMesh);

            cleared = true;
        }

        /// <summary> 現在の描画頂点数 (デバッグ表示用). </summary>
        public int GetVertexCount()
        {
            return initialized ? workerMesh.vertexCount : 0;
        }

        /// <summary>
        /// マスクと重ならないときに切り捨てる.
        /// 枠 (RectTransform) がマスクと重ならなくても、軌跡がマスクと重なるあいだは切り捨てない.
        /// </summary>
        public override void Cull(Rect clipRect, bool validRect)
        {
            var cull = !validRect;

            if (!cull)
            {
                var rect = rectTransform.rect;

                cull = !clipRect.Overlaps(UIParticleSystem.GetRootCanvasRect(canvas, transform, rect.min, rect.max), true) &&
                       !OverlapsTrail(clipRect);
            }

            if (canvasRenderer.cull == cull){ return; }

            canvasRenderer.cull = cull;

            onCullStateChanged.Invoke(cull);

            OnCullingChanged();
        }

        /// <summary> 描いている軌跡の範囲がマスクと重なるか. </summary>
        private bool OverlapsTrail(Rect clipRect)
        {
            if (!initialized || cleared){ return false; }

            var bounds = workerMesh.bounds;

            return clipRect.Overlaps(UIParticleSystem.GetRootCanvasRect(canvas, transform, bounds.min, bounds.max), true);
        }

        protected override void UpdateGeometry()
        {
            // BakeTrailsMesh由来のメッシュを直接SetMeshするため標準のジオメトリ更新は行わない.
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnityUtility.SafeDelete(bakedMesh);
            UnityUtility.SafeDelete(workerMesh);

            initialized = false;
        }
    }
}
