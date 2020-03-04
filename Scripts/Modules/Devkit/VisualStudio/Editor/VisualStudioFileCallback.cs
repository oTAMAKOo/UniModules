
#if ENABLE_VSTU && UNITY_2018_2_OR_NEWER

using System.Collections.Generic;

namespace VisualStudioToolsUnity
{
    public static class VisualStudioFileCallback
    {
        //----- params -----

        public delegate string GeneratedSlnSolutionCallback(string path, string content);

        public delegate string GeneratedCSProjectCallback(string path, string content);

        //----- field -----

        private static List<GeneratedSlnSolutionCallback> generatedSlnSolutionCallbacks = null;

        private static List<GeneratedCSProjectCallback> generatedCSProjectCallbacks = null;

        //----- property -----

        //----- method -----

        public static void AddGeneratedSlnSolutionCallback(GeneratedSlnSolutionCallback callback)
        {
            if (generatedSlnSolutionCallbacks == null)
            {
                generatedSlnSolutionCallbacks = new List<GeneratedSlnSolutionCallback>();
            }

            generatedSlnSolutionCallbacks.Add(callback);
        }

        public static string OnGeneratedSlnSolution(string path, string content)
        {
            if (generatedSlnSolutionCallbacks == null) { return content; }

            foreach (var generatedSlnSolutionCallback in generatedSlnSolutionCallbacks)
            {
                content = generatedSlnSolutionCallback.Invoke(path, content);
            }

            return content;
        }

        public static void AddGeneratedCSProjectCallback(GeneratedCSProjectCallback callback)
        {
            if (generatedCSProjectCallbacks == null)
            {
                generatedCSProjectCallbacks = new List<GeneratedCSProjectCallback>();
            }

            generatedCSProjectCallbacks.Add(callback);
        }

        public static string OnGeneratedCSProject(string path, string content)
        {
            if (generatedCSProjectCallbacks == null) { return content; }

            foreach (var generatedCSProjectCallback in generatedCSProjectCallbacks)
            {
                content = generatedCSProjectCallback.Invoke(path, content);
            }

            return content;
        }
    }
}

#endif
