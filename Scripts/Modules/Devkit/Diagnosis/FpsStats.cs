﻿
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.Devkit.Diagnosis
{
    [ExecuteInEditMode()]
    public class FpsStats : MonoBehaviour
    {
        //----- params -----

        public const float INTERVAL = 0.5f;

        //----- field -----

        [SerializeField]
        private Text fpsLabel = null;

        private float oldTime = 0f;
        private int frame = 0;
        private float frameRate = 0f;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        void Awake()
        {
            Initialize();
        }

        private bool IsEnable()
        {
            return UnityEngine.Debug.isDebugBuild;
        }

        public void Initialize()
        {
            if (!initialized && IsEnable())
            {
                initialized = true;
                oldTime = Time.realtimeSinceStartup;

                SetFrameRate();
            }
        }

        void Update()
        {
            if (initialized && IsEnable())
            {
                frame++;
                float time = Time.realtimeSinceStartup - oldTime;

                if (time >= INTERVAL)
                {
                    frameRate = frame / time;
                    oldTime = Time.realtimeSinceStartup;
                    frame = 0;

                    SetFrameRate();
                }
            }
        }

        private void SetFrameRate()
        {
            fpsLabel.text = frameRate.ToString("0.00") + " fps";
        }
    }
}