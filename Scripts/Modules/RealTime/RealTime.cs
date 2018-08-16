﻿
using Extensions;
using UnityEngine;

namespace Modules.RealTime
{
    public class RealTime : SingletonMonoBehaviour<RealTime>
    {
        //----- params -----


        //----- field -----

        private float realTime = 0f;
        private float realDelta = 0f;

        //----- property -----

        public static float time
        {
            get
            {
                #if UNITY_EDITOR
			    
                if (!Application.isPlaying) return Time.realtimeSinceStartup;
                
                #endif
                
                return Instance.realTime;
            }
        }

        public static float deltaTime
        {
            get
            {
                #if UNITY_EDITOR
			    
                if (!Application.isPlaying) return 0f;
                
                #endif
                return Instance.realDelta;
            }
        }

        //----- method -----

        protected override void Awake()
        {
            base.Awake();
            
            Instance.realTime = Time.realtimeSinceStartup;
        }

        void Update()
        {
            var rt = Time.realtimeSinceStartup;

            realDelta = Mathf.Clamp01(rt - realTime);
            realTime = rt;
        }
    }
}