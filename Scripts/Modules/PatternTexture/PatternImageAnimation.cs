
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.PatternTexture
{
    [ExecuteAlways]
    [RequireComponent(typeof(PatternImage))]
    public abstract class PatternImageAnimation : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField, HideInInspector]
        private int patternIndex = 0;
        [SerializeField, HideInInspector]
        private bool setNativeSize = true;

        private PatternImage target = null;

        private IReadOnlyList<string> patternNames = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            target = UnityUtility.GetComponent<PatternImage>(gameObject);

            patternNames = GetPatternNames();

            if (target != null)
            {
                target.PatternName = patternNames.ElementAtOrDefault(patternIndex);
            }
        }

        void Update()
        {
            if (target == null){ return; }

            var patternName = patternNames.ElementAtOrDefault(patternIndex);

            if (target.PatternName != patternName)
            {
                target.PatternName = patternName;

                if (setNativeSize)
                {
                    target.SetNativeSize();
                }
            }
        }
        
        /// <summary> パターン名取得. </summary>
        public abstract IReadOnlyList<string> GetPatternNames();
    }
}
