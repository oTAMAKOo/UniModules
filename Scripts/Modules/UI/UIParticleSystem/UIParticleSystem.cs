
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
        
        private ParticleSystem.Particle[] particles = null;

        private ParticleSystem.TextureSheetAnimationModule textureSheetAnimation;
        
        private float?[] startFrameValues = null;
        private float?[] overTimeFrameValues = null;

        private int? textureSheetAnimationCurrentFrame = null;

        private Transform transformCache = null;
        private ParticleSystem particleSystemCache = null;
        private ParticleSystemRenderer particleSystemRendererCache = null;
        private ParticleSystem.MainModule? mainModuleCache = null;

        [NonSerialized]
        private bool prevPlaying = false;

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

            Setup();

            particleMaterial = null;

            UpdateRenderingSource();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnityUtility.SafeDelete(particleMaterial);
        }

        private void Initialize()
        {
            if (initialized) { return; }

            raycastTarget = false;

            material = null;

            var particleSystem = GetParticleSystem();
            var mainModule = GetMainModule();

            // disable particle renderer.
            var particleSystemRenderer = GetParticleSystemRenderer();

            particleSystemRenderer.enabled = false;

            // automatically set scaling.         
            mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // limit max  particles.
            mainModule.maxParticles = Mathf.Clamp(mainModule.maxParticles, 0, 10000);

            particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];

            // prepare texture sheet animation.
            textureSheetAnimation = particleSystem.textureSheetAnimation;

            initialized = true;
        }

        private void Setup()
        {
            var mainModule = GetMainModule();

            startFrameValues = new float?[mainModule.maxParticles];
            overTimeFrameValues = new float?[mainModule.maxParticles];
        }

        void Update()
        {
            var particleSystem = GetParticleSystem();

            if (!prevPlaying && particleSystem.isPlaying)
            {
                Setup();
            }

            prevPlaying = particleSystem.isPlaying;

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

                    index = Math.Max(0, Math.Min(index, animModule.spriteCount - 1));

                    var sprite = animModule.GetSprite(index);

                    if (sprite != null)
                    {
                        texture = sprite.texture;
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

            var isSimulationSpaceLocal = mainModule.simulationSpace == ParticleSystemSimulationSpace.Local;

            var count = particleSystem.GetParticles(particles);

            for (var i = 0; i < count; ++i)
            {
                var particle = particles[i];

                // get particle properties.

                var position = isSimulationSpaceLocal ?
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
                var time = 1 - (particle.remainingLifetime / particle.startLifetime);

                var originSeed = UnityEngine.Random.state;

                UnityEngine.Random.InitState((int)particle.randomSeed);

                switch (textureSheetAnimation.mode)
                {
                    case ParticleSystemAnimationMode.Grid:
                        result = GetGridModeUv(particle, index, time);
                        break;

                    case ParticleSystemAnimationMode.Sprites:
                        result = GetSpriteModeUv(particle, index, time);
                        break;
                }

                UnityEngine.Random.state = originSeed;
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
                        textureSheetAnimationFrames = (textureSheetAnimation.numTilesX - 1) * (textureSheetAnimation.numTilesY - 1);
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

        private Vector4 GetGridModeUv(ParticleSystem.Particle particle, int index, float time)
        {
            var result = new Vector4(0, 0, 1, 1);
            
            var numTiles = new Vector2Int(textureSheetAnimation.numTilesX, textureSheetAnimation.numTilesY);

            var textureSheetAnimationFrames = GetTextureSheetAnimationFrames();
            var textureSheetAnimationFrameSize = GetTextureSheetAnimationFrameSize();

            var frame = 0;

            var startFrame = GetStartFrame(index);
            var frameProgress = GetOverTimeFrame(index, time);

            frameProgress = ApplyFrameOverTimeToFrameProgress(particle, frameProgress);

            switch (textureSheetAnimation.animation)
            {
                case ParticleSystemAnimationType.WholeSheet:
                    frame = Mathf.RoundToInt((startFrame + frameProgress) * textureSheetAnimationFrames);
                    break;

                case ParticleSystemAnimationType.SingleRow:
                    {
                        frame = Mathf.FloorToInt((startFrame + frameProgress) * numTiles.x);

                        var row = textureSheetAnimation.rowIndex;
                        
                        if(textureSheetAnimation.rowMode == ParticleSystemAnimationRowMode.Random)
                        {
                            row = UnityEngine.Random.Range(0, numTiles.y);
                        }
                        
                        frame += row * numTiles.x;
                    }
                    break;
            }

            frame = Mathf.Clamp(frame, 0, textureSheetAnimationFrames - 1);

            var frameSize = textureSheetAnimationFrameSize;

            result.x = frame % numTiles.x * frameSize.x;
            result.y = (numTiles.y - Mathf.FloorToInt((float)frame / numTiles.x)) * frameSize.y - frameSize.y;
            result.z = result.x + frameSize.x;
            result.w = result.y + frameSize.y;

            textureSheetAnimationCurrentFrame = frame;

            return result;
        }

        private Vector4 GetSpriteModeUv(ParticleSystem.Particle particle, int index, float time)
        {
            var textureSheetAnimationFrames = GetTextureSheetAnimationFrames();
            
            var startFrame = GetStartFrame(index);
            var frameProgress = GetOverTimeFrame(index, time);

            frameProgress = ApplyFrameOverTimeToFrameProgress(particle, frameProgress);

            var frame = Mathf.RoundToInt((startFrame + frameProgress) * textureSheetAnimationFrames);

            frame = Mathf.Clamp(frame, 0, textureSheetAnimationFrames - 1);

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

        private float ApplyFrameOverTimeToFrameProgress(ParticleSystem.Particle particle, float frameProgress)
        {
            if (textureSheetAnimation.frameOverTime.curveMin != null)
            {
                frameProgress = textureSheetAnimation.frameOverTime.curveMin.Evaluate(1 - (particle.remainingLifetime / particle.startLifetime));
            }
            else if (textureSheetAnimation.frameOverTime.curve != null)
            {
                frameProgress = textureSheetAnimation.frameOverTime.curve.Evaluate(1 - (particle.remainingLifetime / particle.startLifetime));
            }
            else if (textureSheetAnimation.frameOverTime.constant > 0)
            {
                frameProgress = textureSheetAnimation.frameOverTime.constant - (particle.remainingLifetime / particle.startLifetime);
            }

            return frameProgress;
        }

        private float GetStartFrame(int index)
        {
            var frame = 0f;

            var startFrameValue = textureSheetAnimation.startFrame;

            switch (startFrameValue.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    frame = startFrameValue.constant;
                    break;

                case ParticleSystemCurveMode.TwoConstants:
                    {
                        if (!startFrameValues[index].HasValue)
                        {
                            var textureSheetAnimationFrames = GetTextureSheetAnimationFrames();

                            // ※ int引数のUnityEngine.Random.Rangeを使用しないと通常のParticleSystemと挙動が変わってしまう.

                            var constantMin = (int)(startFrameValue.constantMin * textureSheetAnimationFrames);
                            var constantMax = (int)(startFrameValue.constantMax * textureSheetAnimationFrames);

                            startFrameValues[index] = (float)UnityEngine.Random.Range(constantMin, constantMax) / textureSheetAnimationFrames;
                        }

                        frame = startFrameValues[index].Value;
                    }
                    break;

                default:
                    Debug.LogError($"TextureSheetAnimation.StartFrame : {startFrameValue.mode} is not support.");
                    break;
            }

            frame = Mathf.Repeat(frame * textureSheetAnimation.cycleCount, 1);

            return frame;
        }

        private float GetOverTimeFrame(int index, float time)
        {
            var frameOverTimeValue = textureSheetAnimation.frameOverTime;

            var frame = 0f;

            switch (frameOverTimeValue.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    frame = frameOverTimeValue.constant;
                    break;

                case ParticleSystemCurveMode.TwoConstants:
                    {
                        if (!overTimeFrameValues[index].HasValue)
                        {
                            var textureSheetAnimationFrames = GetTextureSheetAnimationFrames();

                            // ※ int引数のUnityEngine.Random.Rangeを使用しないと通常のParticleSystemと挙動が変わってしまう.

                            var constantMin = (int)(frameOverTimeValue.constantMin * textureSheetAnimationFrames);
                            var constantMax = (int)(frameOverTimeValue.constantMax * textureSheetAnimationFrames);

                            overTimeFrameValues[index] = (float)UnityEngine.Random.Range(constantMin, constantMax) / textureSheetAnimationFrames;
                        }

                        frame = overTimeFrameValues[index].Value;
                    }
                    break;

                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    frame = frameOverTimeValue.Evaluate(time, textureSheetAnimation.frameOverTimeMultiplier);
                    break;

                default:
                    Debug.LogError($"TextureSheetAnimation.FrameOverTime : {frameOverTimeValue.mode} is not support.");
                    break;
            }

            return frame; 
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
