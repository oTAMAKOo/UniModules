
using UnityEngine;
using TMPro;

namespace Modules.Devkit.Diagnosis
{
    public sealed class FpsStats : MonoBehaviour
    {
        //----- params -----

        public const float UpdateInterval = 0.5f;

        //----- field -----

        [SerializeField]
        private TextMeshProUGUI fpsLabel = null;
		[SerializeField]
		private string fpsFormat = "{0} fps";

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
			if (initialized) { return; }

			if (!IsEnable()) { return; }

			oldTime = Time.realtimeSinceStartup;

			SetFrameRate();

			initialized = true;
		}

        void Update()
        {
			if (!initialized) { return; }

			if (!IsEnable()) { return; }

			frame++;
			var time = Time.realtimeSinceStartup - oldTime;

			if (time >= UpdateInterval)
			{
				frameRate = frame / time;
				oldTime = Time.realtimeSinceStartup;
				frame = 0;

				SetFrameRate();
			}
		}

        private void SetFrameRate()
        {
            fpsLabel.text = string.Format(fpsFormat, frameRate.ToString("0.00"));
        }
    }
}
