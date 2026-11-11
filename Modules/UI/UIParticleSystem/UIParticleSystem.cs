
// Sourced from - http://forum.unity3d.com/threads/free-script-particle-systems-in-ui-screen-space-overlay.406862/

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.UI.Particle
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    public sealed partial class UIParticleSystem : MaskableGraphic
    {
        //----- params -----

        private const float Tolerance = 0.000001f;

        /// <summary>
        /// 描く粒の数の上限.
        /// 1粒4頂点なので、VertexHelper の頂点数の上限を超えない.
        /// </summary>
        private const int MaxParticleCount = 10000;

        // ※ ParticleSystemRenderer は、粒の種に下の値を足した種から粒ごとの乱数を作る.
        //   同じ値で作り、同じ粒には ParticleSystemRenderer と同じ乱数を出す.

        /// <summary> コマ送りの時間ごとのコマの乱数. </summary>
        private const uint FrameOverTimeRandomId = 0x13740583;

        /// <summary> コマ送りの始まりのコマの乱数. </summary>
        private const uint StartFrameRandomId = 0x56b3dbb0;

        /// <summary> コマ送りの行の乱数. </summary>
        private const uint RowRandomId = 0xaf502044;

        /// <summary> 左右の反転の乱数. </summary>
        private const uint FlipXRandomId = 0x294fb5c7;

        /// <summary> 上下の反転の乱数. </summary>
        private const uint FlipYRandomId = 0x00d4cb82;

        /// <summary> 速さでコマ送りするときに速さの割合に掛ける値 (速さの上限で最後のコマになる). </summary>
        private const float SpeedModeFrameScale = 0.9999f;

        /// <summary>
        /// 描画のあいだ変わらないコマ送りの設定.
        /// 粒ごとに読み直さないよう、粒を描く前に1回だけ読む.
        /// </summary>
        private struct TextureSheetSettings
        {
            public bool Enabled;
            public ParticleSystemAnimationMode Mode;
            public ParticleSystemAnimationTimeMode TimeMode;
            public ParticleSystemAnimationType Animation;
            public ParticleSystemAnimationRowMode RowMode;
            public int RowIndex;
            public Vector2Int NumTiles;
            public int Frames;
            /// <summary> 1 周のコマの数 (Single Row は 1 行のコマの数). </summary>
            public int AnimationFrames;
            public Vector2 FrameSize;
            public int CycleCount;
            public float Fps;
            public Vector2 SpeedRange;
            public ParticleSystem.MinMaxCurve StartFrame;
            public ParticleSystem.MinMaxCurve FrameOverTime;
            /// <summary> スプライトの大きさの基準 (1枚目のスプライトの幅). </summary>
            public float SpriteBaseWidth;
            /// <summary> 時間ごとのコマに粒ごとの乱数を使うか. </summary>
            public bool UseFrameOverTimeRandom;
            /// <summary> 始まりのコマに粒ごとの乱数を使うか. </summary>
            public bool UseStartFrameRandom;
        }

        /// <summary>
        /// 描画のあいだ変わらない描き方の設定.
        /// 粒ごとに読み直さないよう、粒を描く前に1回だけ読む.
        /// </summary>
        private struct RendererSettings
        {
            public ParticleSystemRenderMode RenderMode;
            public Vector3 Pivot;
            public bool UsePivot;
            /// <summary> 反転する粒の割合 (レンダラーの flip. コマ送りの flipU / flipV と同じ値). </summary>
            public Vector2 Flip;
            /// <summary> メッシュを z の向きに反転する粒の割合. </summary>
            public float FlipZ;
            public bool UseFlip;
            public ParticleSystemSortMode SortMode;
            public float LengthScale;
            public float VelocityScale;
            public bool FreeformStretching;
            public bool RotateWithStretchDirection;
            /// <summary> 粒の大きさの下限・上限 (画面の幅に対する割合). </summary>
            public float MinSizeRate;
            public float MaxSizeRate;
            public bool ClampSize;
            /// <summary> Graphic.color を掛けるか. </summary>
            public bool UseColor;
            public Color Color;
        }

        //----- field -----

        [SerializeField]
        private bool useLengthScale = false;
        [SerializeField]
        private bool use3dRotation = false;
        [SerializeField]
        private bool useOverrideMaterial = false;

        private Material particleMaterial = null;

        private Material originMaterial = null;

        private ParticleSystem.Particle[] particles = null;

        /// <summary> 粒1つ分の頂点 (粒ごとに上書きして使い回す). </summary>
        private readonly UIVertex[] quadVertices = new UIVertex[4];

        /// <summary> 並べ替えの値と、並べ替えた粒の番号 (粒を描く順). </summary>
        private float[] sortKeys = null;
        private int[] sortOrder = null;

        private ParticleSystem.TextureSheetAnimationModule textureSheetAnimation;

        private TextureSheetSettings textureSheetSettings = default;

        private RendererSettings rendererSettings = default;

        private Transform transformCache = null;
        private ParticleSystem particleSystemCache = null;
        private ParticleSystemRenderer particleSystemRendererCache = null;
        private ParticleSystem.MainModule? mainModuleCache = null;

        /// <summary> 前回の描画で粒を描いたか (粒が無くなった次の描画で空にする). </summary>
        [NonSerialized]
        private bool particlesDrawn = false;

        [NonSerialized]
        private bool renderModeWarned = false;

        [NonSerialized]
        private bool initialized = false;

        private UIParticleTrail trailCache = null;

        [NonSerialized]
        private bool trailCameraWarned = false;

        //----- property -----

        public override Texture mainTexture
        {
            get { return GetMainTexture(); }
        }

        public ParticleSystem ParticleSystem
        {
            get { return GetParticleSystem(); }
        }

        public bool UseOverrideMaterial
        {
            get { return useOverrideMaterial; }
            set
            {
                if (useOverrideMaterial != value)
                {
                    ResetParticleMaterial();
                }

                useOverrideMaterial = value;
            }
        }

        public bool UseLengthScale
        {
            get { return useLengthScale; }
            set { useLengthScale = value; }
        }

        public bool Use3dRotation
        {
            get { return use3dRotation; }
            set { use3dRotation = value; }
        }

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();

            Initialize();

            // 前回作った複製は破棄して作り直す.
            ResetParticleMaterial();

            UpdateRenderingSource();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // 自分だけを無効にしたときも、トレイルの描画を残さない.
            if (trailCache != null) { trailCache.Clear(); }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnityUtility.SafeDelete(particleMaterial);

            DeleteTrail();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            raycastTarget = false;

            if (Application.isPlaying)
            {
                material = null;
            }

            var particleSystem = GetParticleSystem();
            var mainModule = GetMainModule();

            // disable particle renderer.
            var particleSystemRenderer = GetParticleSystemRenderer();

            particleSystemRenderer.enabled = false;

            // automatically set scaling.
            mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // limit max  particles.
            mainModule.maxParticles = Mathf.Clamp(mainModule.maxParticles, 0, MaxParticleCount);

            particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

            // prepare texture sheet animation.
            textureSheetAnimation = particleSystem.textureSheetAnimation;

            initialized = true;
        }

        void Update()
        {
            var particleSystem = GetParticleSystem();

            UpdateRenderingSource();

            // 粒があるとき (と、粒が無くなった次の描画) だけ作り直す.
            // ※ レイアウトは粒で変わらないので、頂点とマテリアルだけを dirty にする.
            //   マテリアルは、元のマテリアルの値の変化を反映させるため、
            //   粒があるあいだは毎フレーム dirty にする.
            if (particlesDrawn || 0 < particleSystem.particleCount)
            {
                SetVerticesDirty();
                SetMaterialDirty();
            }

            UpdateTrail();
        }

        private Texture GetMainTexture()
        {
            Texture texture = null;

            // コマ送りがスプライトのときは、マテリアルの絵よりスプライトの絵を使う.
            var sprite = GetFirstSheetSprite();

            if (sprite != null)
            {
                texture = sprite.texture;
            }

            if (texture == null && material != null)
            {
                texture = material.mainTexture;
            }

            if (texture == null)
            {
                texture = Texture2D.whiteTexture;
            }

            return texture;
        }

        /// <summary> コマ送りがスプライトのとき、最初のスプライト (無ければ null). </summary>
        private Sprite GetFirstSheetSprite()
        {
            if (!initialized) { return null; }

            var animModule = textureSheetAnimation;

            if (!animModule.enabled) { return null; }

            if (animModule.mode != ParticleSystemAnimationMode.Sprites) { return null; }

            var spriteCount = animModule.spriteCount;

            for (var i = 0; i < spriteCount; ++i)
            {
                var sprite = animModule.GetSprite(i);

                if (sprite != null) { return sprite; }
            }

            return null;
        }

        private void UpdateRenderingSource()
        {
            var particleSystemRenderer = GetParticleSystemRenderer();

            var sharedMaterial = particleSystemRenderer.sharedMaterial;

            if (useOverrideMaterial)
            {
                // 元のマテリアルが替わったら複製を作り直す.
                if (originMaterial != null && originMaterial != sharedMaterial)
                {
                    ResetParticleMaterial();
                }

                // 元のマテリアルが無いときは複製しない.
                if (particleMaterial == null && sharedMaterial != null)
                {
                    particleMaterial = new Material(sharedMaterial)
                    {
                        name = string.Format("{0}(UIParticleSystem)", sharedMaterial.name),
                    };

                    originMaterial = sharedMaterial;
                }

                material = particleMaterial;
            }
            else
            {
                material = sharedMaterial;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // prepare vertices
            vh.Clear();

            #if UNITY_EDITOR

            if (!Application.isPlaying && !initialized)
            {
                Initialize();
            }

            #endif

            particlesDrawn = false;

            if (!gameObject.activeInHierarchy) { return; }

            var particleSystemRenderer = GetParticleSystemRenderer();

            if (particleSystemRenderer.enabled) { return; }

            var particleSystem = GetParticleSystem();
            var mainModule = GetMainModule();

            #if UNITY_EDITOR

            SyncCustomVertexStreams(particleSystemRenderer);

            #endif

            PrepareParticleBuffer(mainModule);

            var count = particleSystem.GetParticles(particles);

            if (count <= 0) { return; }

            particlesDrawn = true;

            // ※ 描画のあいだ変わらない値は、粒ごとに読み直さず粒を描く前に1回だけ読む.
            ReadRendererSettings(particleSystemRenderer);

            if (rendererSettings.RenderMode == ParticleSystemRenderMode.None) { return; }

            ReadTextureSheetSettings();

            ReadViewSettings();

            ReadOrientationSettings(particleSystemRenderer, mainModule);

            var isMesh = rendererSettings.RenderMode == ParticleSystemRenderMode.Mesh;

            if (isMesh)
            {
                ReadParticleMeshes(particleSystemRenderer);

                // メッシュが無いときは描かない.
                if (particleMeshCount <= 0) { return; }
            }

            // ※ Custom Vertex Streams のときは、ストリームの並びで頂点の UV を詰める.
            var useVertexStreams = ReadVertexStreams(particleSystemRenderer, count);

            Matrix4x4 simulationToLocal;

            var isSimulationSpaceLocal = !TryGetSimulationToLocalMatrix(mainModule, out simulationToLocal);
            var isScalingModeShape = mainModule.scalingMode == ParticleSystemScalingMode.Shape;
            var isStretch = rendererSettings.RenderMode == ParticleSystemRenderMode.Stretch;
            var isFreeform = isStretch && rendererSettings.FreeformStretching;
            var isHorizontal = rendererSettings.RenderMode == ParticleSystemRenderMode.HorizontalBillboard;
            var isVertical = rendererSettings.RenderMode == ParticleSystemRenderMode.VerticalBillboard;

            var lengthScale = useLengthScale ? rendererSettings.LengthScale : 1f;

            var stretchLengthScale = isStretch ? rendererSettings.LengthScale : 0f;
            var stretchVelocityScale = isStretch ? rendererSettings.VelocityScale : 0f;

            var orientation = orientationSettings;

            var useVelocity = isStretch || useLengthScale || useVertexStreams || textureSheetSettings.TimeMode == ParticleSystemAnimationTimeMode.Speed ||
                              (!isStretch && orientation.Alignment == ParticleSystemRenderSpace.Velocity);

            // 大きさの上限・下限は、透視投影では粒ごとの奥行きで決まる.
            var sizeLimitPerParticle = rendererSettings.ClampSize && viewSettings.HasCamera && !viewSettings.Orthographic;
            var viewportWidth = rendererSettings.ClampSize && !sizeLimitPerParticle ? GetViewportWidth(Vector3.zero) : 0f;

            var usePivotZ = rendererSettings.Pivot.z != 0f;

            var order = SortParticles(count, isSimulationSpaceLocal, simulationToLocal);

            for (var i = 0; i < count; ++i)
            {
                var particleIndex = order != null ? order[i] : i;

                var particle = particles[particleIndex];

                // 寿命が切れた粒 (トレイルのために残っている粒) は描かない.
                // ※ ParticleSystemRenderer は大きさ 0 の四角にする.
                if (particle.remainingLifetime <= 0f) { continue; }

                // get particle properties.

                var position = isSimulationSpaceLocal ?
                    particle.position :
                    simulationToLocal.MultiplyPoint3x4(particle.position);

                var totalVelocity = Vector3.zero;
                var velocity = Vector3.zero;

                if (useVelocity)
                {
                    totalVelocity = particle.totalVelocity;

                    velocity = isSimulationSpaceLocal ?
                        totalVelocity :
                        simulationToLocal.MultiplyVector(totalVelocity);
                }

                var rotation = -particle.rotation * Mathf.Deg2Rad;
                var rotation3D = particle.rotation3D;

                // ※ ParticleSystemRenderer と同じく、x・y の回転がある粒は常に 3D で回す.
                var is3dRotation = use3dRotation || rotation3D.x != 0f || rotation3D.y != 0f;

                var color = particle.GetCurrentColor(particleSystem);
                var size = particle.GetCurrentSize3D(particleSystem);

                // ※ Size のストリームは、大きさの上限・下限で収める前の大きさ.
                var streamSize = size;

                if (rendererSettings.UseColor)
                {
                    color = (Color)color * rendererSettings.Color;
                }

                if (rendererSettings.ClampSize)
                {
                    var width = sizeLimitPerParticle ? GetViewportWidth(position) : viewportWidth;

                    size = ClampSize(size, rendererSettings.MinSizeRate * width, rendererSettings.MaxSizeRate * width);
                }

                // apply uv.

                var spriteScale = Vector2.one;
                var spriteOffset = Vector2.zero;

                float sheetProgress;

                var particleUv = CalculateUv(particle, totalVelocity.magnitude, ref spriteScale, ref spriteOffset, out sheetProgress);

                // ストリームの次のコマの UV (コマ送りが無いときは同じ UV).
                var nextUv = particleUv;

                if (useVertexStreams)
                {
                    SetParticleStreamValues(particle, particleIndex, position, totalVelocity, streamSize, color, GetSheetFrame(particle, sheetProgress));

                    nextUv = GetNextFrameUv(particle, sheetProgress, particleUv);
                }

                // メッシュの粒は、メッシュの頂点を写して描く (反転は UV ではなくメッシュを鏡に映す).
                if (isMesh)
                {
                    var meshData = GetParticleMeshData(particle);

                    if (meshData == null) { continue; }

                    Matrix4x4 normalMatrix;

                    var meshMatrix = GetParticleMeshMatrix(particle, meshData, position, totalVelocity, size, out normalMatrix);

                    // ※ メッシュの粒の Center のストリームは、pivot でずらしたあとの中心.
                    if (useVertexStreams && rendererSettings.UsePivot)
                    {
                        SetCenterStreamValue(meshMatrix.GetColumn(3));
                    }

                    if (!AddParticleMesh(vh, meshData, meshMatrix, normalMatrix, color, particleUv, useVertexStreams, nextUv)) { break; }

                    continue;
                }

                if (rendererSettings.UseFlip)
                {
                    particleUv = ApplyFlip(particle, particleUv);
                    nextUv = ApplyFlip(particle, nextUv);
                }

                // 回転の中心 (pivot) による、回す前の四角の中心のずれ.
                // レンダラーの pivot は粒の大きさ (z は大きさ.x) を、スプライトの pivot はスプライトに合わせた四角の大きさを単位にする.
                var pivot = rendererSettings.Pivot;
                var pivotOffset = rendererSettings.UsePivot ? new Vector3(pivot.x * size.x, pivot.y * size.y, pivot.z * size.x) : Vector3.zero;
                var usePivot = rendererSettings.UsePivot;

                var particleSize = size;

                // スプライトのコマは、スプライトの大きさに合わせて四角の形を変え、スプライトの pivot で中心をずらす.
                if (spriteOffset != Vector2.zero || spriteScale != Vector2.one)
                {
                    size = new Vector3(size.x * spriteScale.x, size.y * spriteScale.y, size.z);

                    pivotOffset += new Vector3(spriteOffset.x * size.x, spriteOffset.y * size.y, 0f);
                    usePivot = true;
                }

                // ※ AddUIVertexQuad は頂点の値を写すので、同じ配列を粒ごとに上書きして使い回す.
                var quad = quadVertices;

                // ストリームの法線の角の向きを測る位置 (四角と同じ座標の、pivot でずらす前の粒の位置).
                var normalCenter = position;

                if (isHorizontal || isVertical)
                {
                    SetPlanarBillboardQuad(quad, isHorizontal ? orientation.HorizontalFrame : orientation.VerticalFrame, position, particle.rotation, size, rendererSettings.Pivot, color, particleUv);

                    // ※ Horizontal / Vertical Billboard の法線の角の向きは、pivot に関係なく四角の中心から測る.
                    normalCenter = (quad[0].position + quad[2].position) * 0.5f;
                }
                else if (isStretch)
                {
                    // 速度が 0 の粒は伸ばす向きが決まらず面積が無いので描かない.
                    if (velocity.magnitude <= 0f) { continue; }

                    if (isFreeform)
                    {
                        if (!SetFreeformStretchedQuad(quad, position, totalVelocity, size, rotation, stretchLengthScale, stretchVelocityScale, rendererSettings.RotateWithStretchDirection, rendererSettings.Pivot, color, particleUv)) { continue; }
                    }
                    // カメラの面が自分の平面と同じで、速度が平面の中にあるときは平面の中で伸ばす.
                    else if (orientation.FlatView && velocity.z == 0f && !usePivotZ)
                    {
                        if (isScalingModeShape)
                        {
                            position /= canvas.scaleFactor;
                        }

                        // ※ 伸ばすときの pivot は、幅の向きに大きさ.x、伸ばす向きに大きさ.y の 2 倍を単位にする.
                        var stretchPivot = rendererSettings.UsePivot ? new Vector2(pivot.x * particleSize.x, pivot.y * 2f * particleSize.y) : Vector2.zero;

                        SetStretchedMesh(quad, position, size, color, particleUv, velocity, stretchLengthScale, stretchVelocityScale, stretchPivot);
                    }
                    else
                    {
                        var simulationVelocity = totalVelocity;

                        if (!SetOrientedStretchedQuad(quad, position, simulationVelocity, size, stretchLengthScale, stretchVelocityScale, rendererSettings.UsePivot ? pivot : Vector3.zero, color, particleUv)) { continue; }
                    }
                }
                else if (orientation.PerParticle || !orientation.Flat || usePivotZ || (is3dRotation && !orientation.Flat3D))
                {
                    // 自分の平面と違う向きの四角.
                    var frame = orientation.PerParticle ?
                        GetParticleFrame(orientation.LocalToWorld.MultiplyPoint3x4(position), totalVelocity) :
                        orientation.Frame;

                    var rotationDegree = rotation * Mathf.Rad2Deg;
                    var rollAngle = orientation.RollAngle;

                    // 速度の向きに伸ばすときは、四角の向きの面の中での速度の向きに回す.
                    if (useLengthScale)
                    {
                        var x = Vector3.Dot(velocity, frame.X) / Math.Max(frame.X.sqrMagnitude, Tolerance);
                        var y = Vector3.Dot(velocity, frame.Y) / Math.Max(frame.Y.sqrMagnitude, Tolerance);

                        rotationDegree = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                        rollAngle = 0f;
                    }

                    SetOrientedBillboardQuad(quad, position, frame, rotationDegree, is3dRotation, rotation3D, rollAngle, size, lengthScale, usePivot ? pivotOffset : Vector3.zero, color, particleUv);
                }
                else if (is3dRotation)
                {
                    if (usePivot)
                    {
                        position += Quaternion.Euler(rotation3D.x, rotation3D.y, -rotation3D.z) * pivotOffset;
                    }

                    SetBillboard3DRotatedQuad(quad, position, rotation3D, size, color, particleUv);
                }
                else
                {
                    if (useLengthScale)
                    {
                        var normalizedVelocity = velocity.normalized;

                        rotation = Mathf.Atan2(normalizedVelocity.y, normalizedVelocity.x);
                    }

                    if (isScalingModeShape)
                    {
                        position /= canvas.scaleFactor;
                    }

                    normalCenter = position;

                    if (usePivot)
                    {
                        position += RotateOffset(new Vector2(pivotOffset.x * lengthScale, pivotOffset.y), rotation);
                    }

                    // ※ 速度の向きに伸ばすときは、伸ばす向きを揃えるため、
                    //   回転が 0 でも回す経路で描く.
                    if (!useLengthScale && Math.Abs(rotation) < Tolerance)
                    {
                        SetBillboardNotRotatedQuad(quad, position, size, color, particleUv, lengthScale);
                    }
                    else
                    {
                        SetBillboardRotatedQuad(quad, position, rotation, size, color, particleUv, lengthScale);
                    }
                }

                if (useVertexStreams)
                {
                    var isLegacyStretch = isStretch && !isFreeform;

                    var normalMode = isLegacyStretch ? QuadNormalMode.Stretch :
                                     isFreeform || isHorizontal || isVertical ? QuadNormalMode.Planar :
                                     QuadNormalMode.Billboard;

                    var vertexIds = isLegacyStretch ? StretchVertexIds : BillboardVertexIds;

                    SetQuadVertexStreams(quad, vertexIds, normalMode, normalCenter, particleUv, nextUv);

                    AddStreamQuad(vh, quad, vertexIds);
                }
                else
                {
                    vh.AddUIVertexQuad(quad);
                }
            }
        }

        /// <summary> 粒を読む配列を maxParticles まで伸ばす (上限 MaxParticleCount). </summary>
        private void PrepareParticleBuffer(ParticleSystem.MainModule mainModule)
        {
            var size = Mathf.Clamp(mainModule.maxParticles, 0, MaxParticleCount);

            if (particles != null && size <= particles.Length) { return; }

            particles = new ParticleSystem.Particle[size];
        }

        /// <summary>
        /// シミュレーション空間から自分のローカル座標への変換を求める.
        /// ローカル空間 (変換しない) なら false.
        /// </summary>
        private bool TryGetSimulationToLocalMatrix(ParticleSystem.MainModule mainModule, out Matrix4x4 matrix)
        {
            matrix = Matrix4x4.identity;

            switch (mainModule.simulationSpace)
            {
                case ParticleSystemSimulationSpace.World:
                    matrix = GetTransform().worldToLocalMatrix;
                    return true;

                case ParticleSystemSimulationSpace.Custom:
                    {
                        var customSpace = mainModule.customSimulationSpace;

                        // カスタム空間が無いときはローカル空間として扱う.
                        if (customSpace == null) { return false; }

                        matrix = GetTransform().worldToLocalMatrix * customSpace.localToWorldMatrix;
                    }
                    return true;
            }

            return false;
        }

        /// <summary> 描く順に並べた粒の番号 (並べ替えないときは null). </summary>
        private int[] SortParticles(int count, bool isSimulationSpaceLocal, Matrix4x4 simulationToLocal)
        {
            var sortMode = rendererSettings.SortMode;

            if (sortMode == ParticleSystemSortMode.None) { return null; }

            if (sortKeys == null || sortKeys.Length < count)
            {
                var length = Math.Max(count, particles.Length);

                sortKeys = new float[length];
                sortOrder = new int[length];
            }

            // ※ 先に描く粒 (奥に見える粒) ほど値が小さくなるようにする.
            //   古さは ParticleSystemRenderer と同じく残りの寿命で比べる.
            var viewPosition = Vector3.zero;
            var viewDirection = Vector3.forward;
            var isOrthographic = true;

            var useView = sortMode == ParticleSystemSortMode.Distance || sortMode == ParticleSystemSortMode.DistanceReverse ||
                          sortMode == ParticleSystemSortMode.Depth || sortMode == ParticleSystemSortMode.DepthReverse;

            if (useView)
            {
                GetViewInLocal(out viewPosition, out viewDirection, out isOrthographic);
            }

            // ※ 正射影では、距離も奥行きで比べる.
            var isDistance = !isOrthographic && (sortMode == ParticleSystemSortMode.Distance || sortMode == ParticleSystemSortMode.DistanceReverse);
            var isReverse = sortMode == ParticleSystemSortMode.DistanceReverse || sortMode == ParticleSystemSortMode.DepthReverse;

            for (var i = 0; i < count; ++i)
            {
                var particle = particles[i];

                var key = 0f;

                switch (sortMode)
                {
                    case ParticleSystemSortMode.OldestInFront:
                        key = -particle.remainingLifetime;
                        break;

                    case ParticleSystemSortMode.YoungestInFront:
                        key = particle.remainingLifetime;
                        break;

                    default:
                        if (useView)
                        {
                            var position = isSimulationSpaceLocal ?
                                particle.position :
                                simulationToLocal.MultiplyPoint3x4(particle.position);

                            var offset = position - viewPosition;

                            var distance = isDistance ? offset.sqrMagnitude : Vector3.Dot(offset, viewDirection);

                            // 遠い粒から描く (Reverse は近い粒から描く).
                            key = isReverse ? distance : -distance;
                        }
                        break;
                }

                sortKeys[i] = key;
                sortOrder[i] = i;
            }

            Array.Sort(sortKeys, sortOrder, 0, count);

            return sortOrder;
        }

        /// <summary> 見ている位置と向き (自分のローカル座標) と、正射影か. </summary>
        private void GetViewInLocal(out Vector3 viewPosition, out Vector3 viewDirection, out bool isOrthographic)
        {
            var view = viewSettings;

            var transform = GetTransform();

            viewDirection = transform.InverseTransformDirection(view.Forward);

            if (view.HasCamera)
            {
                viewPosition = transform.InverseTransformPoint(view.Position);
                isOrthographic = view.Orthographic;
            }
            else
            {
                // カメラが無いときは、キャンバスの正面から正射影で見ているとする.
                viewPosition = Vector3.zero;
                isOrthographic = true;
            }
        }

        /// <summary>
        /// 速度の向きに伸ばした四角 (粒の位置が先頭で、後ろへ伸ばす).
        /// ParticleSystemRenderer と同じく、粒の回転は使わない.
        /// pivotOffset: 回転の中心による、回す前の四角のずれ (x: 幅の向き, y: 伸ばす向き).
        /// </summary>
        private static void SetStretchedMesh(UIVertex[] quad, Vector3 position, Vector2 size, Color32 color, Vector4 uv,
                                             Vector3 velocity, float lengthScale, float velocityScale, Vector2 pivotOffset)
        {
            var scaleFactor = CalculateScale(velocity);

            var width = size.x;
            var height = CalculateHeight(size, velocity, lengthScale, velocityScale);

            SetModel(quad, width, height, scaleFactor, color, uv);

            if (pivotOffset != Vector2.zero)
            {
                var offset = new Vector3(pivotOffset.x, pivotOffset.y * scaleFactor, 0f);

                for (var i = 0; i < 4; ++i)
                {
                    quad[i].position += offset;
                }
            }

            var finalAngle = CalculateAngle(velocity);

            ApplyPositionAndRotationTransform(quad, position, finalAngle);
        }

        /// <summary> 伸ばした長さ (大きさ × lengthScale に、速さ × velocityScale を足す). </summary>
        private static float CalculateHeight(Vector2 size, Vector3 velocity, float lengthScale, float velocityScale)
        {
            return size.y * lengthScale + velocityScale * velocity.magnitude;
        }

        private static float CalculateScale(Vector3 velocity)
        {
            var cubeX = velocity.x * velocity.x;
            var cubeY = velocity.y * velocity.y;

            var v3M = velocity.magnitude;
            var v2M = Math.Sqrt(cubeX + cubeY);

            var scaleFactor = (float)v2M / v3M;

            return scaleFactor;
        }

        /// <summary> 上向きから速度の向きまでの角度 (ラジアン. 反時計回り). </summary>
        private static float CalculateAngle(Vector3 velocity)
        {
            return Mathf.Atan2(-velocity.x, velocity.y);
        }

        /// <summary> 粒の大きさを下限・上限に収める (縦横の比は保つ. 上限が 0 以下のときは収めない). </summary>
        private static Vector3 ClampSize(Vector3 size, float minSize, float maxSize)
        {
            var largest = Math.Max(size.x, size.y);

            if (largest <= 0f || maxSize <= 0f) { return size; }

            var clamped = Mathf.Clamp(largest, minSize, maxSize);

            if (Math.Abs(clamped - largest) < Tolerance) { return size; }

            return size * (clamped / largest);
        }

        /// <summary> 回す前のずれを、粒と一緒に回す (rotation: ラジアン. 反時計回り). </summary>
        private static Vector3 RotateOffset(Vector2 offset, float rotation)
        {
            var sin = Mathf.Sin(rotation);
            var cos = Mathf.Cos(rotation);

            return new Vector3(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos, 0f);
        }

        /// <summary> 粒ごとの乱数で、UV を左右・上下に反転する (四角の形は反転しない). </summary>
        private Vector4 ApplyFlip(ParticleSystem.Particle particle, Vector4 uv)
        {
            var flip = rendererSettings.Flip;

            if (0f < flip.x && GenerateRandom(particle.randomSeed + FlipXRandomId) < flip.x)
            {
                uv = new Vector4(uv.z, uv.y, uv.x, uv.w);
            }

            if (0f < flip.y && GenerateRandom(particle.randomSeed + FlipYRandomId) < flip.y)
            {
                uv = new Vector4(uv.x, uv.w, uv.z, uv.y);
            }

            return uv;
        }

        /// <summary>
        /// 粒の UV を求める.
        /// コマの番号は ParticleSystemRenderer と同じく、始まりのコマと時間ごとのコマを足して 1 で折り返し、
        /// コマの数を掛けて切り捨てる.
        /// </summary>
        private Vector4 CalculateUv(ParticleSystem.Particle particle, float speed, ref Vector2 spriteScale, ref Vector2 spriteOffset, out float progress)
        {
            var result = new Vector4(0, 0, 1, 1);

            progress = 0f;

            if (textureSheetSettings.Enabled)
            {
                progress = GetSheetProgress(particle, speed);

                switch (textureSheetSettings.Mode)
                {
                    case ParticleSystemAnimationMode.Grid:
                        result = GetGridModeUv(particle, progress);
                        break;

                    case ParticleSystemAnimationMode.Sprites:
                        result = GetSpriteModeUv(progress, ref spriteScale, ref spriteOffset);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// シートの中の位置 (0〜1).
        /// 寿命で進めるときは、寿命の割合に cycleCount を掛けて折り返した時間で時間ごとのコマを読む.
        /// 速さで進めるときは、速さの割合に cycleCount を掛ける (時間ごとのコマは読まない).
        /// FPS で進めるときは、生まれてからの秒数で fps コマずつ進める.
        /// </summary>
        private float GetSheetProgress(ParticleSystem.Particle particle, float speed)
        {
            var settings = textureSheetSettings;

            var startFrameRandom = settings.UseStartFrameRandom ? GenerateRandom(particle.randomSeed + StartFrameRandomId) : 0f;

            var startFrame = settings.StartFrame.Evaluate(0f, startFrameRandom);

            var frame = 0f;

            switch (settings.TimeMode)
            {
                case ParticleSystemAnimationTimeMode.FPS:
                    {
                        var age = particle.startLifetime - particle.remainingLifetime;

                        frame = age * settings.Fps / settings.AnimationFrames;
                    }
                    break;

                case ParticleSystemAnimationTimeMode.Speed:
                    {
                        var time = Mathf.InverseLerp(settings.SpeedRange.x, settings.SpeedRange.y, speed);

                        frame = time * settings.CycleCount * SpeedModeFrameScale;
                    }
                    break;

                default:
                    {
                        var age = 1 - (particle.remainingLifetime / particle.startLifetime);

                        var time = Mathf.Repeat(age * settings.CycleCount, 1f);

                        var frameOverTimeRandom = settings.UseFrameOverTimeRandom ? GenerateRandom(particle.randomSeed + FrameOverTimeRandomId) : 0f;

                        frame = settings.FrameOverTime.Evaluate(time, frameOverTimeRandom);
                    }
                    break;
            }

            return Mathf.Repeat(startFrame + frame, 1f);
        }

        /// <summary> 描画のあいだ変わらないコマ送りの設定を読む. </summary>
        private void ReadTextureSheetSettings()
        {
            var settings = new TextureSheetSettings()
            {
                Enabled = textureSheetAnimation.enabled,
            };

            if (settings.Enabled)
            {
                settings.Mode = textureSheetAnimation.mode;
                settings.TimeMode = textureSheetAnimation.timeMode;
                settings.Animation = textureSheetAnimation.animation;
                settings.RowMode = textureSheetAnimation.rowMode;
                settings.RowIndex = textureSheetAnimation.rowIndex;
                settings.NumTiles = new Vector2Int(textureSheetAnimation.numTilesX, textureSheetAnimation.numTilesY);
                settings.Frames = GetTextureSheetAnimationFrames();
                settings.AnimationFrames = settings.Mode == ParticleSystemAnimationMode.Grid && settings.Animation == ParticleSystemAnimationType.SingleRow ?
                    settings.NumTiles.x :
                    settings.Frames;
                settings.FrameSize = GetTextureSheetAnimationFrameSize();
                settings.CycleCount = textureSheetAnimation.cycleCount;
                settings.Fps = textureSheetAnimation.fps;
                settings.SpeedRange = textureSheetAnimation.speedRange;

                // ※ モジュールの MinMaxCurve は読むたびに曲線のオブジェクトが作られる.
                settings.StartFrame = textureSheetAnimation.startFrame;
                settings.FrameOverTime = textureSheetAnimation.frameOverTime;

                settings.UseStartFrameRandom = IsRandomCurveMode(settings.StartFrame.mode);
                settings.UseFrameOverTimeRandom = IsRandomCurveMode(settings.FrameOverTime.mode);

                if (settings.Mode == ParticleSystemAnimationMode.Sprites)
                {
                    var sprite = GetFirstSheetSprite();

                    settings.SpriteBaseWidth = sprite != null ? sprite.rect.width / sprite.pixelsPerUnit : 0f;
                }

                // スプライトが無いときはコマ送りしない.
                if (settings.Frames <= 0 || settings.AnimationFrames <= 0)
                {
                    settings.Enabled = false;
                }
            }

            textureSheetSettings = settings;
        }

        /// <summary> 描画のあいだ変わらない描き方の設定を読む. </summary>
        private void ReadRendererSettings(ParticleSystemRenderer particleSystemRenderer)
        {
            var settings = new RendererSettings()
            {
                RenderMode = particleSystemRenderer.renderMode,
                Pivot = particleSystemRenderer.pivot,
                SortMode = particleSystemRenderer.sortMode,
                LengthScale = particleSystemRenderer.lengthScale,
                VelocityScale = particleSystemRenderer.velocityScale,
                Color = color,
            };

            switch (settings.RenderMode)
            {
                case ParticleSystemRenderMode.Billboard:
                case ParticleSystemRenderMode.Stretch:
                case ParticleSystemRenderMode.HorizontalBillboard:
                case ParticleSystemRenderMode.VerticalBillboard:
                case ParticleSystemRenderMode.Mesh:
                case ParticleSystemRenderMode.None:
                    break;

                default:
                    // 未対応の描き方は Billboard で描く (ログは1度だけ出す).
                    if (!renderModeWarned)
                    {
                        renderModeWarned = true;

                        Debug.LogError($"UIParticleSystem : RenderMode {settings.RenderMode} is not supported. (rendered as Billboard)");
                    }

                    settings.RenderMode = ParticleSystemRenderMode.Billboard;
                    break;
            }

            if (settings.RenderMode == ParticleSystemRenderMode.Stretch)
            {
                settings.FreeformStretching = particleSystemRenderer.freeformStretching;
                settings.RotateWithStretchDirection = particleSystemRenderer.rotateWithStretchDirection;
            }

            settings.UsePivot = settings.Pivot != Vector3.zero;

            // ※ コマ送りの flipU / flipV はレンダラーの flip と同じ値.
            //   z の反転はメッシュにだけ使う.
            var flip = particleSystemRenderer.flip;

            settings.Flip = flip;
            settings.FlipZ = settings.RenderMode == ParticleSystemRenderMode.Mesh ? flip.z : 0f;
            settings.UseFlip = 0f < flip.x || 0f < flip.y || 0f < settings.FlipZ;

            settings.UseColor = settings.Color != Color.white;

            // ※ 大きさの下限・上限は ParticleSystemRenderer と同じく画面の幅に対する割合で、Billboard・Horizontal / Vertical Billboard の粒にだけ使う.
            //   画面の幅が分からない (キャンバスの下に無い) ときは使わない.
            var isBillboard = settings.RenderMode == ParticleSystemRenderMode.Billboard ||
                              settings.RenderMode == ParticleSystemRenderMode.HorizontalBillboard ||
                              settings.RenderMode == ParticleSystemRenderMode.VerticalBillboard;

            if (isBillboard && canvas != null)
            {
                settings.MinSizeRate = particleSystemRenderer.minParticleSize;
                settings.MaxSizeRate = particleSystemRenderer.maxParticleSize;
                settings.ClampSize = true;
            }

            rendererSettings = settings;
        }

        private static bool IsRandomCurveMode(ParticleSystemCurveMode mode)
        {
            return mode == ParticleSystemCurveMode.TwoConstants || mode == ParticleSystemCurveMode.TwoCurves;
        }

        /// <summary> ParticleSystemRenderer が粒ごとに作る乱数 (0〜1) を、同じ種から求める. </summary>
        private static float GenerateRandom(uint seed)
        {
            uint x, y, z, w;

            InitializeRandom(seed, out x, out y, out z, out w);

            return NextRandom(ref x, ref y, ref z, ref w);
        }

        /// <summary> ParticleSystemRenderer が同じ種から続けて作る乱数 (0〜1) の、最初の3つ. </summary>
        private static void GenerateRandoms(uint seed, out float first, out float second, out float third)
        {
            uint x, y, z, w;

            InitializeRandom(seed, out x, out y, out z, out w);

            first = NextRandom(ref x, ref y, ref z, ref w);
            second = NextRandom(ref x, ref y, ref z, ref w);
            third = NextRandom(ref x, ref y, ref z, ref w);
        }

        private static void InitializeRandom(uint seed, out uint x, out uint y, out uint z, out uint w)
        {
            x = seed;
            y = x * 1812433253u + 1u;
            z = y * 1812433253u + 1u;
            w = z * 1812433253u + 1u;
        }

        private static float NextRandom(ref uint x, ref uint y, ref uint z, ref uint w)
        {
            var t = x ^ (x << 11);

            x = y;
            y = z;
            z = w;
            w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

            return (w & 0x007FFFFF) * (1f / 8388607f);
        }

        public void ResetParticleMaterial()
        {
            UnityUtility.SafeDelete(particleMaterial);

            particleMaterial = null;
            originMaterial = null;
        }

        #region Cull

        /// <summary>
        /// マスクと重ならないときに切り捨てる.
        /// 枠 (RectTransform) がマスクと重ならなくても、粒がマスクと重なるあいだは切り捨てない.
        /// </summary>
        public override void Cull(Rect clipRect, bool validRect)
        {
            var cull = !validRect;

            if (!cull)
            {
                var rect = rectTransform.rect;

                cull = !clipRect.Overlaps(GetRootCanvasRect(canvas, GetTransform(), rect.min, rect.max), true) &&
                       !OverlapsParticles(clipRect);
            }

            if (canvasRenderer.cull == cull) { return; }

            canvasRenderer.cull = cull;

            onCullStateChanged.Invoke(cull);

            OnCullingChanged();
        }

        /// <summary> 粒の範囲がマスクと重なるか. </summary>
        private bool OverlapsParticles(Rect clipRect)
        {
            if (!initialized) { return false; }

            var particleSystem = GetParticleSystem();

            if (particleSystem.particleCount <= 0) { return false; }

            var particleSystemRenderer = GetParticleSystemRenderer();

            if (particleSystemRenderer.enabled) { return false; }

            var mainModule = GetMainModule();

            PrepareParticleBuffer(mainModule);

            var count = particleSystem.GetParticles(particles);

            if (count <= 0) { return false; }

            Matrix4x4 simulationToLocal;

            var isSimulationSpaceLocal = !TryGetSimulationToLocalMatrix(mainModule, out simulationToLocal);

            var pivot = particleSystemRenderer.pivot;

            // ※ 回転 (3D の向きを含む)・回転の中心・伸ばす分を含めて、粒の中心から角までの長さを大きめに見積もる.
            var extentRate = 0.75f + Math.Max(Math.Abs(pivot.x), Math.Max(Math.Abs(pivot.y), Math.Abs(pivot.z))) * 2f;

            var isStretch = particleSystemRenderer.renderMode == ParticleSystemRenderMode.Stretch;
            var isMesh = particleSystemRenderer.renderMode == ParticleSystemRenderMode.Mesh;

            // メッシュの粒は、メッシュの原点から最も遠い頂点と回転の中心のずれまでを含める.
            if (isMesh)
            {
                ReadParticleMeshes(particleSystemRenderer);

                extentRate = GetParticleMeshRadius() + pivot.magnitude;
            }

            var lengthScale = isStretch || useLengthScale ? particleSystemRenderer.lengthScale : 1f;
            var velocityScale = isStretch ? particleSystemRenderer.velocityScale : 0f;

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            var overlaps = false;

            for (var i = 0; i < count; ++i)
            {
                var particle = particles[i];

                // 寿命が切れた粒は描かないので範囲に含めない.
                if (particle.remainingLifetime <= 0f) { continue; }

                overlaps = true;

                var position = isSimulationSpaceLocal ?
                    particle.position :
                    simulationToLocal.MultiplyPoint3x4(particle.position);

                var size = particle.GetCurrentSize3D(particleSystem);

                var maxSize = isMesh ? Math.Max(size.x, Math.Max(size.y, size.z)) : Math.Max(size.x, size.y);

                var extent = maxSize * extentRate * Math.Max(1f, lengthScale);

                if (isStretch)
                {
                    var velocity = isSimulationSpaceLocal ?
                        particle.totalVelocity :
                        simulationToLocal.MultiplyVector(particle.totalVelocity);

                    extent += velocityScale * velocity.magnitude;
                }

                min = Vector3.Min(min, position - new Vector3(extent, extent, extent));
                max = Vector3.Max(max, position + new Vector3(extent, extent, extent));
            }

            if (!overlaps) { return false; }

            return clipRect.Overlaps(GetRootCanvasRect(canvas, GetTransform(), min, max), true);
        }

        /// <summary> ローカル座標の範囲 (箱) を、ルートのキャンバスの座標の範囲にする. </summary>
        internal static Rect GetRootCanvasRect(Canvas canvas, Transform transform, Vector3 min, Vector3 max)
        {
            var matrix = transform.localToWorldMatrix;

            if (canvas != null)
            {
                matrix = canvas.rootCanvas.transform.worldToLocalMatrix * matrix;
            }

            var rectMin = new Vector2(float.MaxValue, float.MaxValue);
            var rectMax = new Vector2(float.MinValue, float.MinValue);

            // 奥行きの無い範囲は 4 隅、ある範囲は 8 隅を写す.
            var cornerCount = min.z == max.z ? 4 : 8;

            for (var i = 0; i < cornerCount; ++i)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);

                Vector2 point = matrix.MultiplyPoint(corner);

                rectMin = Vector2.Min(rectMin, point);
                rectMax = Vector2.Max(rectMax, point);
            }

            return new Rect(rectMin, rectMax - rectMin);
        }

        #endregion

        #region Trail

        /// <summary> Trail描画の更新. TrailModule有効時は隠し子のUIParticleTrailを自動生成して描画する. </summary>
        private void UpdateTrail()
        {
            var particleSystem = GetParticleSystem();

            if (!particleSystem.trails.enabled)
            {
                // 生成済みの場合は描画をクリアして待機 (再有効化に備えて破棄はしない).
                if (trailCache != null){ trailCache.Clear(); }

                return;
            }

            var trail = GetOrCreateTrail();

            if (trail == null){ return; }

            trail.UpdateTrail(GetBakeCamera(), maskable);
        }

        /// <summary> 管理下のUIParticleTrailを取得 (未生成時はnull・エディタのデバッグ表示でも使用). </summary>
        public UIParticleTrail GetTrail()
        {
            if (trailCache != null){ return trailCache; }

            UIParticleTrail result = null;

            List<UIParticleTrail> duplicates = null;

            // 直下の子のみ検索 (孫のUIParticleSystemが生成したTrailを誤取得しないため).
            for (var i = 0; i < transform.childCount; i++)
            {
                var trail = UnityUtility.GetComponent<UIParticleTrail>(transform.GetChild(i).gameObject);

                if (trail == null){ continue; }

                if (result == null)
                {
                    result = trail;
                }
                else
                {
                    // リロード等の残留で重複していた場合は最初の1つを採用し残りは破棄対象にする.
                    if (duplicates == null){ duplicates = new List<UIParticleTrail>(); }

                    duplicates.Add(trail);
                }
            }

            if (duplicates != null)
            {
                // 走査完了後に破棄 (エディタでは即時破棄となり走査中の子順が変わるため分離する).
                foreach (var duplicate in duplicates)
                {
                    UnityUtility.SafeDelete(duplicate.gameObject);
                }
            }

            if (result != null)
            {
                // リロードで参照が切れた残留物を再利用するため設定し直す.
                result.Setup(GetParticleSystem(), GetParticleSystemRenderer());
            }

            trailCache = result;

            return trailCache;
        }

        private UIParticleTrail GetOrCreateTrail()
        {
            var trail = GetTrail();

            if (trail != null){ return trail; }

            var trailObject = new GameObject("Trail (UIParticleSystem)");

            // Hierarchyに表示せずシーン・プレハブにも保存しない (必要時に自動再生成される).
            trailObject.hideFlags = HideFlags.HideAndDontSave;

            trailObject.layer = gameObject.layer;

            var trailTransform = trailObject.AddComponent<RectTransform>();

            trailTransform.SetParent(transform, false);
            trailTransform.anchoredPosition3D = Vector3.zero;
            trailTransform.localRotation = Quaternion.identity;
            trailTransform.localScale = Vector3.one;

            // 親にLayoutGroupがある場合にレイアウト対象へ含めない.
            var layoutElement = trailObject.AddComponent<LayoutElement>();

            layoutElement.ignoreLayout = true;

            trailCache = trailObject.AddComponent<UIParticleTrail>();

            trailCache.Setup(GetParticleSystem(), GetParticleSystemRenderer());

            return trailCache;
        }

        private void DeleteTrail()
        {
            if (trailCache == null){ return; }

            UnityUtility.SafeDelete(trailCache.gameObject);

            trailCache = null;
        }

        /// <summary> Trailベイク用カメラを取得. </summary>
        private Camera GetBakeCamera()
        {
            var currentCanvas = canvas;

            if (currentCanvas == null){ return null; }

            var rootCanvas = currentCanvas.rootCanvas;

            // Screen Space - Overlayはベイク用カメラが存在しないためTrail描画不可.
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (!trailCameraWarned)
                {
                    trailCameraWarned = true;

                    Debug.LogError("UIParticleTrail requires Screen Space - Camera or World Space canvas. (Screen Space - Overlay is not supported)");
                }

                return null;
            }

            var bakeCamera = rootCanvas.worldCamera;

            // カメラが設定されていないキャンバスでは Trail をベイクできない.
            if (bakeCamera == null && !trailCameraWarned)
            {
                trailCameraWarned = true;

                Debug.LogError("UIParticleTrail requires a camera on the canvas. (Trail is not rendered)");
            }

            return bakeCamera;
        }

        #endregion

        #region TextureSheetAnimation

        private int GetTextureSheetAnimationFrames()
        {
            var textureSheetAnimationFrames = 0;

            if (textureSheetAnimation.enabled)
            {
                switch (textureSheetAnimation.mode)
                {
                    case ParticleSystemAnimationMode.Grid:
                        textureSheetAnimationFrames = textureSheetAnimation.numTilesX * textureSheetAnimation.numTilesY;
                        break;

                    case ParticleSystemAnimationMode.Sprites:
                        textureSheetAnimationFrames = textureSheetAnimation.spriteCount;
                        break;
                }
            }

            return textureSheetAnimationFrames;
        }

        private Vector2 GetTextureSheetAnimationFrameSize()
        {
            var textureSheetAnimationFrameSize = Vector2.zero;

            if (textureSheetAnimation.enabled)
            {
                if (textureSheetAnimation.mode == ParticleSystemAnimationMode.Grid)
                {
                    textureSheetAnimationFrameSize = new Vector2(1f / textureSheetAnimation.numTilesX, 1f / textureSheetAnimation.numTilesY);
                }
            }

            return textureSheetAnimationFrameSize;
        }

        private Vector4 GetGridModeUv(ParticleSystem.Particle particle, float progress)
        {
            var numTiles = textureSheetSettings.NumTiles;

            var textureSheetAnimationFrames = textureSheetSettings.Frames;

            var frame = 0;

            switch (textureSheetSettings.Animation)
            {
                case ParticleSystemAnimationType.WholeSheet:
                    frame = Mathf.FloorToInt(progress * textureSheetAnimationFrames);
                    break;

                case ParticleSystemAnimationType.SingleRow:
                    {
                        var column = Mathf.Clamp(Mathf.FloorToInt(progress * numTiles.x), 0, numTiles.x - 1);

                        frame = GetSheetRow(particle) * numTiles.x + column;
                    }
                    break;
            }

            frame = Mathf.Clamp(frame, 0, textureSheetAnimationFrames - 1);

            return GetGridFrameUv(frame);
        }

        /// <summary> Single Row のコマ送りの行. </summary>
        private int GetSheetRow(ParticleSystem.Particle particle)
        {
            var numTiles = textureSheetSettings.NumTiles;

            var row = textureSheetSettings.RowIndex;

            switch (textureSheetSettings.RowMode)
            {
                case ParticleSystemAnimationRowMode.Random:
                    row = Mathf.FloorToInt(GenerateRandom(particle.randomSeed + RowRandomId) * numTiles.y);
                    break;

                case ParticleSystemAnimationRowMode.MeshIndex:
                    row = GetParticleMeshIndex(particle);
                    break;
            }

            return Mathf.Clamp(row, 0, numTiles.y - 1);
        }

        /// <summary> Grid のコマの UV (コマの番号はシートの範囲に収めない). </summary>
        private Vector4 GetGridFrameUv(int frame)
        {
            var numTiles = textureSheetSettings.NumTiles;

            var frameSize = textureSheetSettings.FrameSize;

            var result = new Vector4();

            result.x = frame % numTiles.x * frameSize.x;
            result.y = (numTiles.y - Mathf.FloorToInt((float)frame / numTiles.x)) * frameSize.y - frameSize.y;
            result.z = result.x + frameSize.x;
            result.w = result.y + frameSize.y;

            return result;
        }

        /// <summary> コマ送りのコマの位置 (小数を含む. AnimFrame・AnimBlend のストリームの値). </summary>
        private float GetSheetFrame(ParticleSystem.Particle particle, float progress)
        {
            if (!textureSheetSettings.Enabled) { return 0f; }

            if (textureSheetSettings.Mode == ParticleSystemAnimationMode.Grid && textureSheetSettings.Animation == ParticleSystemAnimationType.SingleRow)
            {
                var columns = textureSheetSettings.NumTiles.x;

                return GetSheetRow(particle) * columns + progress * columns;
            }

            return progress * textureSheetSettings.Frames;
        }

        /// <summary> 次のコマの UV (UV2 のストリームの値. コマ送りが無いときは今の UV). </summary>
        private Vector4 GetNextFrameUv(ParticleSystem.Particle particle, float progress, Vector4 uv)
        {
            if (!textureSheetSettings.Enabled) { return uv; }

            var frame = Mathf.FloorToInt(GetSheetFrame(particle, progress)) + 1;

            if (textureSheetSettings.Mode == ParticleSystemAnimationMode.Sprites)
            {
                var spriteScale = Vector2.one;
                var spriteOffset = Vector2.zero;

                // ※ コマの真ん中の位置を渡して、割り算の誤差で前のコマにならないようにする.
                return GetSpriteModeUv((frame + 0.5f) / textureSheetSettings.Frames, ref spriteScale, ref spriteOffset);
            }

            return GetGridFrameUv(frame);
        }

        /// <summary>
        /// スプライトのコマの UV.
        /// あわせて、粒の大きさに掛ける値と、スプライトの pivot による四角の中心のずれ (大きさに対する割合) を求める.
        /// </summary>
        private Vector4 GetSpriteModeUv(float progress, ref Vector2 spriteScale, ref Vector2 spriteOffset)
        {
            var textureSheetAnimationFrames = textureSheetSettings.Frames;

            var frame = Mathf.Clamp(Mathf.FloorToInt(progress * textureSheetAnimationFrames), 0, textureSheetAnimationFrames - 1);

            var sprite = textureSheetAnimation.GetSprite(frame);

            // スプライトが無いときはテクスチャ全体を使う.
            if (sprite == null || sprite.texture == null) { return new Vector4(0, 0, 1, 1); }

            // ※ 1枚目のスプライトの幅を粒の大きさとし、縦横の比と大きさの違いを保つ.
            var spriteRect = sprite.rect;
            var baseWidth = textureSheetSettings.SpriteBaseWidth;

            if (0f < baseWidth && 0f < spriteRect.width && 0f < spriteRect.height)
            {
                spriteScale = spriteRect.size / (sprite.pixelsPerUnit * baseWidth);

                var pivot = sprite.pivot;

                spriteOffset = new Vector2(0.5f - pivot.x / spriteRect.width, 0.5f - pivot.y / spriteRect.height);
            }

            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var textureRect = sprite.textureRect;

            var result = new Vector4()
            {
                x = textureRect.xMin / textureSize.x,
                y = textureRect.yMin / textureSize.y,
                z = textureRect.xMax / textureSize.x,
                w = textureRect.yMax / textureSize.y
            };

            return result;
        }

        #endregion

        #region Quad

        private const float HalfPi = Mathf.PI / 2;

        private static void SetBillboardNotRotatedQuad(UIVertex[] quad, Vector3 position, Vector2 size, Color32 color, Vector4 uv, float lengthScale)
        {
            var corner1 = new Vector2(position.x - size.x * 0.5f, position.y - size.y * 0.5f * lengthScale);
            var corner2 = new Vector2(position.x + size.x * 0.5f, position.y + size.y * 0.5f * lengthScale);

            quad[0].position = new Vector3(corner1.x, corner1.y, position.z);
            quad[1].position = new Vector3(corner1.x, corner2.y, position.z);
            quad[2].position = new Vector3(corner2.x, corner2.y, position.z);
            quad[3].position = new Vector3(corner2.x, corner1.y, position.z);

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);
        }

        private static void SetBillboardRotatedQuad(UIVertex[] quad, Vector3 position, float rotation, Vector2 size, Color32 color, Vector4 uv, float lengthScale)
        {
            var rotation90 = rotation + HalfPi;

            var right = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * size.x * 0.5f * lengthScale;
            var up = new Vector2(Mathf.Cos(rotation90), Mathf.Sin(rotation90)) * size.y * 0.5f;

            var center = (Vector2)position;

            quad[0].position = ToPosition(center - right - up, position.z);
            quad[1].position = ToPosition(center - right + up, position.z);
            quad[2].position = ToPosition(center + right + up, position.z);
            quad[3].position = ToPosition(center + right - up, position.z);

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);
        }

        /// <summary> 3D に回した四角 (position: 粒の位置. 自分のローカル座標). </summary>
        private static void SetBillboard3DRotatedQuad(UIVertex[] quad, Vector3 position, Vector3 rotation3D, Vector2 size, Color32 color, Vector4 uv)
        {
            var vert0 = new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0f);
            var vert1 = new Vector3(-size.x * 0.5f,  size.y * 0.5f, 0f);
            var vert2 = new Vector3( size.x * 0.5f,  size.y * 0.5f, 0f);
            var vert3 = new Vector3( size.x * 0.5f, -size.y * 0.5f, 0f);

            // ※ z の回転は、ParticleSystemRenderer (と 2D の経路) と同じ向きにするため符号を反転する.
            var particleRotation = Quaternion.Euler(rotation3D.x, rotation3D.y, -rotation3D.z);

            quad[0].position = position + particleRotation * vert0;
            quad[1].position = position + particleRotation * vert1;
            quad[2].position = position + particleRotation * vert2;
            quad[3].position = position + particleRotation * vert3;

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);
        }

        public static UIVertex[] GenerateModel(float width, float height, float scalefactor, Color32 color, Vector4 uv)
        {
            var quad = new UIVertex[4];

            SetModel(quad, width, height, scalefactor, color, uv);

            return quad;
        }

        private static void SetModel(UIVertex[] quad, float width, float height, float scalefactor, Color32 color, Vector4 uv)
        {
            var halfWidth = width / 2;
            var scaledHeight = -height * scalefactor;

            quad[0].position = new Vector2(-halfWidth, scaledHeight);
            quad[1].position = new Vector2(-halfWidth, 0);
            quad[2].position = new Vector2(halfWidth, 0);
            quad[3].position = new Vector2(halfWidth, scaledHeight);

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.z, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.y);
            quad[2].uv0 = new Vector2(uv.x, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.w);
        }

        private static void ApplyPositionAndRotationTransform(UIVertex[] quad, Vector3 position, float angle)
        {
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);

            quad[0].position = ToPosition(GetRotatedAndMovedPoint(quad[0].position, sin, cos, position), position.z);
            quad[1].position = ToPosition(GetRotatedAndMovedPoint(quad[1].position, sin, cos, position), position.z);
            quad[2].position = ToPosition(GetRotatedAndMovedPoint(quad[2].position, sin, cos, position), position.z);
            quad[3].position = ToPosition(GetRotatedAndMovedPoint(quad[3].position, sin, cos, position), position.z);
        }

        private static Vector3 ToPosition(Vector2 position, float z)
        {
            return new Vector3(position.x, position.y, z);
        }

        private static Vector2 GetRotatedAndMovedPoint(Vector2 curr, double sin, double cos, Vector2 pos)
        {
            var x = curr.x * cos - curr.y * sin + pos.x;
            var y = curr.x * sin + curr.y * cos + pos.y;

            return new Vector2((float)x, (float)y);
        }

        #endregion

        #region Cache

        private Transform GetTransform()
        {
            if (transformCache == null)
            {
                transformCache = UnityUtility.GetComponent<Transform>(gameObject);
            }

            return transformCache;
        }

        private ParticleSystem GetParticleSystem()
        {
            if (particleSystemCache == null)
            {
                particleSystemCache = UnityUtility.GetComponent<ParticleSystem>(gameObject);
            }

            return particleSystemCache;
        }

        private ParticleSystemRenderer GetParticleSystemRenderer()
        {
            if (particleSystemRendererCache == null)
            {
                var particleSystem = GetParticleSystem();

                particleSystemRendererCache = particleSystem.GetComponent<ParticleSystemRenderer>();
            }

            return particleSystemRendererCache;
        }

        private ParticleSystem.MainModule GetMainModule()
        {
            if (mainModuleCache == null)
            {
                var particleSystem = GetParticleSystem();

                mainModuleCache = particleSystem.main;
            }

            return mainModuleCache.Value;
        }

        #endregion
    }
}
