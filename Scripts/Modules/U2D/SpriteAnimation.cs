
using UnityEngine;
using UnityEngine.U2D;
using System;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.U2D
{
    public sealed class AtlasTextureAnimation : MonoBehaviour
    {
        //----- params -----

        public enum State
        {
            Play,
            Stop,
        }

        //----- field -----

        [SerializeField]
        private SpriteAtlas spriteAtlas = null;
        [SerializeField]
        private float updateInterval = 1f;

        private int animationIndex = 0;
        private Sprite[] animationSprites = null;
        private float animationTime = 0f;
        
        private Subject<Sprite> onUpdateAnimation = null;

        //----- property -----

        public State CurrentState { get; private set; }

        public string CurrentAnimation { get; private set; }

        public SpriteAtlas SpriteAtlas
        {
            get { return spriteAtlas; }
            set { spriteAtlas = value; }
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
            CurrentState = State.Play;
            animationTime = 0f;
            Loop = loop;

            CurrentAnimation = animationName;

            LoadSprites(CurrentAnimation);

            animationIndex = startIndex.HasValue ? startIndex.Value : 0;

            if (onUpdateAnimation != null)
            {
                onUpdateAnimation.OnNext(animationSprites[animationIndex]);
            }
        }

        public void Stop()
        {
            CurrentState = State.Stop;
        }

        private void LoadSprites(string animationName)
        {
            var index = 0;
            var sprites = new List<Sprite>();

            while (true)
            {
                var spriteName = string.Format("{0}_{1}", animationName, index);
                
                var sprite = spriteAtlas.GetSprite(spriteName);
                
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
                else
                {
                    break;
                }

                index++;
            }

            animationSprites = sprites.ToArray();
        }

        public IObservable<Sprite> OnUpdateAnimationAsObservable()
        {
            return onUpdateAnimation ?? (onUpdateAnimation = new Subject<Sprite>());
        }
    }
}
