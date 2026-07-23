
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

        private Mesh bakedMesh = null;

        private Mesh workerMesh = null;

        private CombineInstance[] combineInstances = null;

        private Material currentTrailMaterial = null;

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

        /// <summary> Trailメッシュを更新 (UIParticleSystemから毎フレーム駆動される). </summary>
        public void UpdateTrail(Camera bakeCamera, bool maskable)
        {
            if (!initialized){ return; }

            if (particleSystem == null){ return; }
            if (particleSystemRenderer == null){ return; }

            this.maskable = maskable;

            // ベイク用カメラがない場合 (Screen Space - Overlay等) は描画不可.
            if (bakeCamera == null)
            {
                Clear();
                return;
            }

            // 粒子がなければTrailも存在しない.
            if (particleSystem.particleCount <= 0)
            {
                Clear();
                return;
            }

            bakedMesh.Clear(false);

            // レンダラー無効状態ではベイクできないため一時的に有効化する.
            var rendererEnabled = particleSystemRenderer.enabled;

            particleSystemRenderer.enabled = true;

            particleSystemRenderer.BakeTrailsMesh(bakedMesh, bakeCamera, ParticleSystemBakeMeshOptions.BakeRotationAndScale);

            particleSystemRenderer.enabled = rendererEnabled;

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

            workerMesh.Clear(false);

            canvasRenderer.SetMesh(workerMesh);
        }

        /// <summary> 現在の描画頂点数 (デバッグ表示用). </summary>
        public int GetVertexCount()
        {
            return initialized ? workerMesh.vertexCount : 0;
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
