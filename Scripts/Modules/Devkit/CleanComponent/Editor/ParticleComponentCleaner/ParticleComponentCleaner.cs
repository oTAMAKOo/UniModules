
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System.Linq;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.UI.Particle;

namespace Modules.Devkit.CleanComponent
{
    public abstract class ParticleComponentCleaner
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoClean
            {
                get { return ProjectPrefs.GetBool("ParticleComponentCleanerPrefs-autoClean", true); }
                set { ProjectPrefs.SetBool("ParticleComponentCleanerPrefs-autoClean", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        protected static void ModifyParticleSystemComponent(GameObject rootObject)
        {
            var particleSystems = rootObject.DescendantsAndSelf().OfComponent<ParticleSystem>();

            foreach (var particleSystem in particleSystems)
            {
                if (!CheckModifyTarget(particleSystem)) { continue; }

                var particleSystemRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();

                //------ ShapeModule ------

                var shapeModule = particleSystem.shape;
                
                if (shapeModule.enabled)
                {
                    if (shapeModule.shapeType != ParticleSystemShapeType.Mesh)
                    {
                        shapeModule.mesh = null;
                    }

                    if (shapeModule.shapeType != ParticleSystemShapeType.MeshRenderer)
                    {
                        shapeModule.meshRenderer = null;
                    }

                    if (shapeModule.shapeType != ParticleSystemShapeType.SkinnedMeshRenderer)
                    {
                        shapeModule.skinnedMeshRenderer = null;
                    }

                    if (shapeModule.shapeType != ParticleSystemShapeType.Sprite)
                    {
                        shapeModule.sprite = null;
                    }

                    if (shapeModule.shapeType != ParticleSystemShapeType.SpriteRenderer)
                    {
                        shapeModule.spriteRenderer = null;
                    }                
                }            
                else
                {
                    shapeModule.mesh = null;
                    shapeModule.meshRenderer = null;
                    shapeModule.skinnedMeshRenderer = null;
                    shapeModule.sprite = null;
                    shapeModule.spriteRenderer = null;
                    shapeModule.texture = null;
                }

                //------ CollisionModule ------

                var collisionModule = particleSystem.collision;

                if (!collisionModule.enabled || collisionModule.type != ParticleSystemCollisionType.Planes)
                {
                    for (var i = 0; i < collisionModule.planeCount; i++)
                    {
                        collisionModule.SetPlane(i, null);
                    }
                }

                //------ TriggerModule ------

                var triggerModule = particleSystem.trigger;

                if (!triggerModule.enabled)
                {
                    for (var i = 0; i < triggerModule.colliderCount; i++)
                    {
                        triggerModule.SetCollider(i, null);
                    }
                }

                //------ TextureSheetAnimationModule ------

                var textureSheetAnimationModule = particleSystem.textureSheetAnimation;

                if (!textureSheetAnimationModule.enabled || textureSheetAnimationModule.mode != ParticleSystemAnimationMode.Sprites)
                {
                    for (var i = 0; i < textureSheetAnimationModule.spriteCount; i++)
                    {
                        textureSheetAnimationModule.RemoveSprite(i);
                    }
                }

                //------ LightsModule ------

                var lightsModule = particleSystem.lights;

                if (!lightsModule.enabled)
                {
                    lightsModule.light = null;
                }

                //------ TrailModule ------

                if (!particleSystem.trails.enabled)
                {
                    particleSystemRenderer.trailMaterial = null;
                }

                //------ Renderer ------

                var uiParticleSystem = UnityUtility.GetComponent<UIParticleSystem>(particleSystem.gameObject);

                if (uiParticleSystem == null && !particleSystemRenderer.enabled)
                {
                    particleSystemRenderer.sharedMaterials = new Material[0];
                }

                EditorUtility.SetDirty(particleSystem);
            }
        }

        protected static bool CheckExecute(GameObject[] gameObjects)
        {
            var modify = gameObjects
                .SelectMany(x => x.DescendantsAndSelf().OfComponent<ParticleSystem>())
                .Any(x => CheckModifyTarget(x));

            if (modify)
            {
                return EditorUtility.DisplayDialog("ParticleSystem Cleaner", "ParticleSystem is directly Do you want to run cleanup?", "Execute", "Cancel");
            }

            return false;
        }

        private static bool CheckModifyTarget(ParticleSystem particleSystem)
        {
            var particleSystemRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            //------ ShapeModule ------

            var shapeModule = particleSystem.shape;
            
            if (shapeModule.enabled)
            {
                if (shapeModule.shapeType != ParticleSystemShapeType.Mesh && shapeModule.mesh != null) { return true; }
                if (shapeModule.shapeType != ParticleSystemShapeType.MeshRenderer && shapeModule.meshRenderer != null) { return true; }
                if (shapeModule.shapeType != ParticleSystemShapeType.SkinnedMeshRenderer && shapeModule.skinnedMeshRenderer != null) { return true; }
                if (shapeModule.shapeType != ParticleSystemShapeType.Sprite && shapeModule.sprite != null) { return true; }
                if (shapeModule.shapeType != ParticleSystemShapeType.SpriteRenderer && shapeModule.spriteRenderer != null) { return true; }
            }            
            else
            {
                if (shapeModule.mesh != null) { return true; }
                if (shapeModule.meshRenderer != null) { return true; }
                if (shapeModule.skinnedMeshRenderer != null) { return true; }
                if (shapeModule.sprite != null) { return true; }
                if (shapeModule.spriteRenderer != null) { return true; }
                if (shapeModule.texture != null) { return true; }
            }

            //------ CollisionModule ------

            var collisionModule = particleSystem.collision;

            if (!collisionModule.enabled || collisionModule.type != ParticleSystemCollisionType.Planes)
            {
                for (var i = 0; i < collisionModule.planeCount; i++)
                {
                    var plane = collisionModule.GetPlane(i);

                    if (plane != null) { return true; }
                }
            }

            //------ TriggerModule ------

            var triggerModule = particleSystem.trigger;

            if (!triggerModule.enabled)
            {
                for (var i = 0; i < triggerModule.colliderCount; i++)
                {
                    var collider = triggerModule.GetCollider(i);

                    if (collider != null) { return true; }
                }
            }

            //------ TextureSheetAnimationModule ------

            var textureSheetAnimationModule = particleSystem.textureSheetAnimation;

            if (!textureSheetAnimationModule.enabled || textureSheetAnimationModule.mode != ParticleSystemAnimationMode.Sprites)
            {
                for (var i = 0; i < textureSheetAnimationModule.spriteCount; i++)
                {
                    var sprite = textureSheetAnimationModule.GetSprite(i);

                    if (sprite != null) { return true; }
                }
            }

            //------ LightsModule ------

            if (!particleSystem.lights.enabled && particleSystem.lights.light != null) { return true; }

            //------ TrailModule ------

            if (!particleSystem.trails.enabled && particleSystemRenderer.trailMaterial != null) { return true; }

            //------ Renderer ------

            var uiParticleSystem = UnityUtility.GetComponent<UIParticleSystem>(particleSystem.gameObject);

            if (uiParticleSystem == null)
            {
                if (!particleSystemRenderer.enabled && particleSystemRenderer.sharedMaterials.Any(x => x != null)) { return true; }
            }

            return false;
        }
    }
}
