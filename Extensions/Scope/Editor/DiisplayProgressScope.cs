
using UnityEngine;
using UnityEditor;

namespace Extensions
{
    public sealed class DiisplayProgressScope : Scope
    {
        private string title = null;

        private float max = 0f;

        public DiisplayProgressScope(string title, string info, float max)
        {
            SetTitle(title);
            SetMax(max);

            if (!Application.isBatchMode)
            {
                Set(info, 0);
            }
        }

        protected override void CloseScope()
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void SetTitle(string title) 
        {
            this.title = title;
        }

        public void SetMax(float max) 
        {
            this.max = max;
        }

        public void Set(string info, float progress)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar(title, info, progress / max);
            }
        }
    }
}
