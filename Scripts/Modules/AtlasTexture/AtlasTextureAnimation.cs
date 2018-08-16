
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Atlas
{
    public class AtlasTextureAnimation : MonoBehaviour
    {
        //----- params -----

        public enum State
        {
            Play,
            Stop,
        }

        //----- field -----

        [SerializeField]
        private AtlasTexture sourceAtlas = null;
        [SerializeField]
        private float updateInterval = 1f;

        private State currentState = State.Stop;
        private string currentAnimation = null;
        private int animationIndex = 0;
        private Sprite[] animationSprites = null;
        private float animationTime = 0f;

        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        private Subject<Sprite> onUpdateAnimation = null;

        //----- property -----

        public State CurrentState { get { return currentState; } }

        public string CurrentAnimation
        {
            get { return currentAnimation; }
        }

        public AtlasTexture SourceAtlas
        {
            get { return sourceAtlas; }
            set { sourceAtlas = value; }
        }

        public float UpdateInterval
        {
            get { return updateInterval; }
            set { updateInterval = value; }
        }

        public bool Loop { get; set; }

        //----- method -----

        void Update()
        {
            if (CurrentState != State.Play) { return; }

            if (animationSprites.IsEmpty()) { return; }

            var time = Time.deltaTime;

            if (updateInterval < animationTime)
            {
                animationIndex++;

                if (animationSprites.Length <= animationIndex)
                {
                    animationIndex = 0;

                    if (Loop == false)
                    {
                        Stop();
                    }
                }

                if (onUpdateAnimation != null)
                {
                    onUpdateAnimation.OnNext(animationSprites[animationIndex]);
                }

                animationTime = 0f;
            }
            else
            {
                animationTime += time;
            }
        }

        public void Play(string animationName, int? startIndex = null, bool loop = true)
        {
            currentState = State.Play;
            animationTime = 0f;
            Loop = loop;

            currentAnimation = animationName;

            LoadSprites(currentAnimation);

            animationIndex = startIndex.HasValue ? startIndex.Value : 0;

            if (onUpdateAnimation != null)
            {
                onUpdateAnimation.OnNext(animationSprites[animationIndex]);
            }
        }

        public void Stop()
        {
            currentState = State.Stop;
        }

        public void ClearSpriteCache()
        {
            spriteCache.Clear();
        }

        private void LoadSprites(string animationName)
        {
            var sprites = new List<Sprite>();
            var count = sourceAtlas.GetListOfSprites().Count(x => x.StartsWith(animationName));

            for (var i = 0; i < count; i++)
            {
                var spriteName = string.Format("{0}_{1}", animationName, i);

                Sprite sprite = null;

                sprite = spriteCache.GetValueOrDefault(spriteName);

                if (sprite != null)
                {
                    sprites.Add(sprite);
                    continue;
                }

                sprite = sourceAtlas.GetSprite(spriteName);

                if (sprite != null)
                {
                    spriteCache.Add(spriteName, sprite);
                    sprites.Add(sprite);
                }
                else
                {
                    break;
                }
            }

            animationSprites = sprites.ToArray();
        }

        public IObservable<Sprite> OnUpdateAnimationAsObservable()
        {
            return onUpdateAnimation ?? (onUpdateAnimation = new Subject<Sprite>());
        }
    }
}