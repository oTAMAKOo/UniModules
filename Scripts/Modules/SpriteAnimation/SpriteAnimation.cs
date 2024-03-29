﻿
using UnityEngine;
using UnityEngine.U2D;
using System;
using UniRx;
using Extensions;
using Modules.Cache;

namespace Modules.SpriteAnimation
{
    public class SpriteAnimation : MonoBehaviour
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

        private int currentIndex = 0;
        private int animationCount = 0;
        private float animationTime = 0f;

        private SpriteAtlasCache spriteCache = null;

        private Subject<Sprite> onUpdateAnimation = null;

        private Subject<Unit> onLastFrame = null;

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

        public string AnimationName { get; private set; }

        public bool Loop { get; set; }

        //----- method -----

        void Update()
        {
            if (CurrentState != State.Play) { return; }         

            var time = Time.deltaTime;

            if (updateInterval < animationTime)
            {
                currentIndex++;

                if (animationCount <= currentIndex)
                {
                    currentIndex = 0;

                    if (onLastFrame != null)
                    {
                        onLastFrame.OnNext(Unit.Default);
                    }

                    if (Loop == false)
                    {
                        Stop();
                    }
                }

                UpdateAnimation(currentIndex);

                animationTime = 0f;
            }
            else
            {
                animationTime += time;
            }
        }

        public void Play(string animationName, int? startIndex = null, bool loop = true)
        {
            AnimationName = animationName;
            CurrentState = State.Play;
            animationTime = 0f;
            Loop = loop;

            CurrentAnimation = animationName;

            LoadSprites();

            currentIndex = startIndex.HasValue ? startIndex.Value : 0;

            UpdateAnimation(currentIndex);
        }

        public void Stop()
        {
            CurrentState = State.Stop;
        }

        private void LoadSprites()
        {
            if (spriteAtlas == null) { return; }

            var referenceName = $"SpriteAnimation.{spriteAtlas.GetInstanceID()}";

            spriteCache = new SpriteAtlasCache(spriteAtlas, referenceName);

            animationCount = 0;

            while (true)
            {
                var spriteName = GetSpriteName(animationCount);
                
                var sprite = spriteCache.GetSprite(spriteName);
                
                if (sprite == null) { break; }

                animationCount++;
            }
        }

        private void UpdateAnimation(int index)
        {
            var spriteName = GetSpriteName(index);

            var sprite = spriteCache.GetSprite(spriteName);

            if (sprite != null)
            {
                if (onUpdateAnimation != null)
                {
                    onUpdateAnimation.OnNext(sprite);
                }
            }
        }

        private string GetSpriteName(int index)
        {
            return $"{AnimationName}_{index}";
        }

        public IObservable<Sprite> OnUpdateAnimationAsObservable()
        {
            return onUpdateAnimation ?? (onUpdateAnimation = new Subject<Sprite>());
        }

        public IObservable<Unit> OnLastFrameAsObservable()
        {
            return onLastFrame ?? (onLastFrame = new Subject<Unit>());
        }
    }
}
