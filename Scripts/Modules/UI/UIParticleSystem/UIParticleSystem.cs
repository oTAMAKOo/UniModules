
// Sourced from - http://forum.unity3d.com/threads/free-script-particle-systems-in-ui-screen-space-overlay.406862/

using UnityEngine;
using UnityEngine.UI;
using System;
using Extensions;

namespace Modules.UI.Particle
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    public sealed class UIParticleSystem : MaskableGraphic
    {
        //----- params -----

        private const float Tolerance = 0.000001f;

        //----- field -----

        [SerializeField]
        private bool useOverrideMaterial = false;

        private Material particleMaterial = null;

        private Material originMaterial = null;

        private int?[] startFrames = null;
        private ParticleSystem.Particle[] particles = null;

        private ParticleSystem.TextureSheetAnimationModule textureSheetAnimation;
        private int? textureSheetAnimationCurrentFrame = null;

        private Transform transformCache = null;
        private ParticleSystem particleSystemCache = null;
        private ParticleSystemRenderer particleSystemRendererCache = null;
        private ParticleSystem.MainModule? mainModuleCache = null;

        [NonSerialized]
        private bool initialized = false;

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

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();

            Initialize();
            
            particleMaterial = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnityUtility.SafeDelete(particleMaterial);
        }

        private void Initialize()
        {
            if (initialized) { return; }

            var particleSystem = GetParticleSystem();
            var mainModule = GetMainModule();

            // limit max  particles.
            mainModule.maxParticles = Mathf.Clamp(mainModule.maxParticles, 0, 10000);

            // automatically set scaling.         
            mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;

            startFrames = new int?[particleSystem.main.maxParticles];
            
            particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

            // prepare texture sheet animation.
            textureSheetAnimation = particleSystem.textureSheetAnimation;

            raycastTarget = false;

            // disable particle renderer.
            var particleSystemRenderer = GetParticleSystemRenderer();

            particleSystemRenderer.enabled = false;

            initialized = true;
        }

        void Update()
        {
            UpdateRenderingSource();

            SetAllDirty();
        }

        private Texture GetMainTexture()
        {
            Texture texture = null;

            if (material != null)
            {
                texture = material.mainTexture;
            }

            if (texture == null)
            {
                var animModule = textureSheetAnimation;

                if (animModule.enabled && textureSheetAnimationCurrentFrame.HasValue)
                {
                    var index = textureSheetAnimationCurrentFrame.Value;

                    if (animModule.spriteCount < index)
                    {
                        index = animModule.spriteCount;
                    }

                    var sprite = animModule.GetSprite(index);

                    if (sprite != null)
                    {
                        texture = sprite.texture;
                    }
                    else
                    {
                        var hierarchyPath = UnityUtility.GetHierarchyPath(gameObject);

                        Debug.LogWarningFormat("TextureSheetAnimation sprite index not found.\n\nindex : {0}\nhierarchy : {1}\n", index, hierarchyPath);
                    }
                }
            }

            if (texture == null)
            {
                texture = Texture2D.whiteTexture;
            }

            return texture;
        }

        private void UpdateRenderingSource()
        {
            var particleSystemRenderer = GetParticleSystemRenderer();
            
            if (useOverrideMaterial)
            {
                if (originMaterial != null)
                {
                    var materialReset = false;

                    materialReset |= originMaterial != particleSystemRenderer.sharedMaterial;
                    materialReset |= originMaterial.shader != particleSystemRenderer.sharedMaterial.shader;

                    if (materialReset)
                    {
                        ResetParticleMaterial();
                    }
                }

                if (particleMaterial == null)
                {
                    particleMaterial = new Material(particleSystemRenderer.sharedMaterial)
                    {
                        name = string.Format("{0}(UIParticleSystem)", particleSystemRenderer.sharedMaterial.name),
                    };

                    originMaterial = particleSystemRenderer.sharedMaterial;
                }

                material = particleMaterial;
            }
            else
            {
                material = particleSystemRenderer.sharedMaterial;
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

            if (!gameObject.activeInHierarchy) { return; }

            var particleSystemRenderer = GetParticleSystemRenderer();

            if (particleSystemRenderer.enabled) { return; }

            var transform = GetTransform();
            var particleSystem = GetParticleSystem();
            var mainModule = GetMainModule();

            var count = particleSystem.GetParticles(particles);

            for (var i = 0; i < count; ++i)
            {
                var particle = particles[i];

                // get particle properties.

                var position = mainModule.simulationSpace == ParticleSystemSimulationSpace.Local ?
                    particle.position :
                    transform.InverseTransformPoint(particle.position);

                var rotation = -particle.rotation * Mathf.Deg2Rad;
                var color = particle.GetCurrentColor(particleSystem);
                var size = particle.GetCurrentSize3D(particleSystem);

                // apply scale.

                if (mainModule.scalingMode == ParticleSystemScalingMode.Shape)
                {
                    position /= canvas.scaleFactor;
                }

                // apply uv.

                var particleUv = CalculateUv(i, particle);

                UIVertex[] quad;

                if (particleSystemRendererCache.renderMode == ParticleSystemRenderMode.Stretch)
                {
                    quad = GenerateStretchedMesh(color, size, position, rotation, particleUv, i);
                }
                else if (Math.Abs(rotation) < Tolerance)
                {
                    quad = GetBillboardNotRotatedQuad(position, size, color, particleUv);
                }
                else
                {
                    quad = GetBillboardRotatedQuad(position, rotation, size, color, particleUv);
                }

                vh.AddUIVertexQuad(quad);
            }
        }

        private UIVertex[] GenerateStretchedMesh(Color32 meshColor, Vector2 size, Vector2 position, float rotation, Vector4 uv, int particleId)
        {
            var velocity = particles[particleId].velocity;
            var scaleFactor = CalculateScale(velocity);

            var width = size.x;
            var height = CalculateHeight(size, velocity);

            var quad = GenerateModel(width, height, scaleFactor, meshColor, uv);
            var finalAngle = CalculateAngle(velocity, rotation);

            quad = ApplyPositionAndRotationTransform(quad, position, finalAngle);

            return quad;
        }

        private float CalculateHeight(Vector2 size, Vector3 velocity)
        {
            var particleSystemRenderer = GetParticleSystemRenderer();

            var stretched = particleSystemRenderer.lengthScale;
            var velocityStretched = particleSystemRenderer.velocityScale;
            var stretchedScale = stretched + velocityStretched * velocity.magnitude;

            return stretchedScale * size.y;
        }

        private float CalculateScale(Vector3 velocity)
        {
            var cubeX = velocity.x * velocity.x;
            var cubeY = velocity.y * velocity.y;

            var v3M = velocity.magnitude;
            var v2M = Math.Sqrt(cubeX + cubeY);

            var scaleFactor = (float)v2M / v3M;

            return scaleFactor;
        }

        private static float CalculateAngle(Vector3 velocity, float rotation)
        {
            var up = Vector2.up;
            var velocity2 = new Vector2(velocity.x, velocity.y);
            var angle = Vector2.Angle(velocity2, up);
            var finalAngle = velocity2.x > 0 ? -angle : angle;

            finalAngle *= Mathf.Deg2Rad;
            finalAngle += rotation;

            return finalAngle;
        }

        private Vector4 CalculateUv(int index, ParticleSystem.Particle particle)
        {
            var result = new Vector4(0, 0, 1, 1);

            textureSheetAnimationCurrentFrame = null;

            if (textureSheetAnimation.enabled)
            {
                var frameOverTimeModule = textureSheetAnimation.frameOverTime;

                var time = 1 - particle.remainingLifetime * textureSheetAnimation.cycleCount / particle.startLifetime;
                
                var frameProgress = frameOverTimeModule.Evaluate(time);
                
                switch (textureSheetAnimation.mode)
                {
                    case ParticleSystemAnimationMode.Grid:
                        result = GetGridModeUv(index, frameProgress, particle.randomSeed);
                        break;

                    case ParticleSystemAnimationMode.Sprites:
                        result = GetSpriteModeUv(index, frameProgress);
                        break;
                }
            }

            return result;
        }

        public void ResetParticleMaterial()
        {
            UnityUtility.SafeDelete(particleMaterial);

            particleMaterial = null;
            originMaterial = null;
        }

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

        private int GetTextureSheetAnimationStartFrame(int index)
        {
            var startFrame = startFrames[index];

            if (!startFrame.HasValue)
            {
                switch (textureSheetAnimation.startFrame.mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        startFrame = Mathf.FloorToInt(textureSheetAnimation.startFrame.constant);
                        break;
                        
                    case ParticleSystemCurveMode.TwoConstants:
                        var constantMin = textureSheetAnimation.startFrame.constantMin;
                        var constantMax = textureSheetAnimation.startFrame.constantMax;
                        startFrame = Mathf.FloorToInt(UnityEngine.Random.Range(constantMin, constantMax));
                        break;
                    default:
                        startFrame = 0;
                        break;
                }

                startFrames[index] = startFrame;
            }

            return startFrame.Value;
        }

        private Vector4 GetGridModeUv(int index, float frameProgress, uint randomSeed)
        {
            var result = new Vector4(0, 0, 1, 1);
            
            var numTiles = new Vector2Int(textureSheetAnimation.numTilesX, textureSheetAnimation.numTilesY);

            var textureSheetAnimationFrames = GetTextureSheetAnimationFrames();
            var textureSheetAnimationFrameSize = GetTextureSheetAnimationFrameSize();

            var startFrame = GetTextureSheetAnimationStartFrame(index);

            var frame = 0;

            switch (textureSheetAnimation.animation)
            {
                case ParticleSystemAnimationType.WholeSheet:
                    frame = Mathf.RoundToInt(frameProgress * textureSheetAnimationFrames);
                    break;

                case ParticleSystemAnimationType.SingleRow:
                    {
                        frame = Mathf.FloorToInt(frameProgress * numTiles.x);

                        var row = textureSheetAnimation.rowIndex;
                        
                        if(textureSheetAnimation.rowMode == ParticleSystemAnimationRowMode.Random)
                        {
                            var originSeed = UnityEngine.Random.state;

                            UnityEngine.Random.InitState((int)randomSeed);

                            row = UnityEngine.Random.Range(0, numTiles.y);

                            UnityEngine.Random.state = originSeed;
                        }
                        
                        frame += row * numTiles.x;
                    }
                    break;
            }

            frame = Mathf.Clamp(startFrame + frame, 0, textureSheetAnimationFrames - 1);

            var frameSize = textureSheetAnimationFrameSize;

            result.x = frame % numTiles.x * frameSize.x;
            result.y = (numTiles.y - Mathf.FloorToInt((float)frame / numTiles.x)) * frameSize.y - frameSize.y;
            result.z = result.x + frameSize.x;
            result.w = result.y + frameSize.y;

            textureSheetAnimationCurrentFrame = frame;

            return result;
        }

        private Vector4 GetSpriteModeUv(int index, float frameProgress)
        {
            var textureSheetAnimationFrames = GetTextureSheetAnimationFrames();

            var startFrame = GetTextureSheetAnimationStartFrame(index);
            
            var frame = Mathf.RoundToInt(frameProgress * textureSheetAnimationFrames);

            frame = Mathf.Clamp(startFrame + frame, 0, textureSheetAnimationFrames - 1);

            var sprite = textureSheetAnimation.GetSprite(frame);

            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var textureRect = sprite.textureRect;

            var result = new Vector4()
            {
                x = textureRect.xMin / textureSize.x,
                y = textureRect.yMin / textureSize.y,
                z = textureRect.xMax / textureSize.x,
                w = textureRect.yMax / textureSize.y
            };

            textureSheetAnimationCurrentFrame = frame;

            return result;
        }

        #endregion

        #region Quad

        private const float HalfPi = Mathf.PI / 2;

        private static UIVertex[] GetBillboardNotRotatedQuad(Vector2 position, Vector2 size, Color32 color, Vector4 uv)
        {
            var quad = new UIVertex[4];
            var corner1 = new Vector2(position.x - size.x * 0.5f, position.y - size.y * 0.5f);
            var corner2 = new Vector2(position.x + size.x * 0.5f, position.y + size.y * 0.5f);

            quad[0].position = new Vector2(corner1.x, corner1.y);
            quad[1].position = new Vector2(corner1.x, corner2.y);
            quad[2].position = new Vector2(corner2.x, corner2.y);
            quad[3].position = new Vector2(corner2.x, corner1.y);

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);

            return quad;
        }

        private static UIVertex[] GetBillboardRotatedQuad(Vector2 position, float rotation, Vector2 size, Color32 color, Vector4 uv)
        {
            var quad = new UIVertex[4];
            var rotation90 = rotation + HalfPi;

            var right = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * size.x * 0.5f;
            var up = new Vector2(Mathf.Cos(rotation90), Mathf.Sin(rotation90)) * size.y * 0.5f;

            quad[0].position = position - right - up;
            quad[1].position = position - right + up;
            quad[2].position = position + right + up;
            quad[3].position = position + right - up;

            quad[0].color = color;
            quad[1].color = color;
            quad[2].color = color;
            quad[3].color = color;

            quad[0].uv0 = new Vector2(uv.x, uv.y);
            quad[1].uv0 = new Vector2(uv.x, uv.w);
            quad[2].uv0 = new Vector2(uv.z, uv.w);
            quad[3].uv0 = new Vector2(uv.z, uv.y);

            return quad;
        }

        public static UIVertex[] GenerateModel(float width, float height, float scalefactor, Color32 color, Vector4 uv)
        {
            var quad = new UIVertex[4];
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

            return quad;
        }

        private static UIVertex[] ApplyPositionAndRotationTransform(UIVertex[] quad, Vector2 position, float angle)
        {
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);

            quad[0].position = GetRotatedAndMovedPoint(quad[0].position, sin, cos, position);
            quad[1].position = GetRotatedAndMovedPoint(quad[1].position, sin, cos, position);
            quad[2].position = GetRotatedAndMovedPoint(quad[2].position, sin, cos, position);
            quad[3].position = GetRotatedAndMovedPoint(quad[3].position, sin, cos, position);

            return quad;
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
