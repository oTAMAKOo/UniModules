
using System;

namespace Modules.BehaviorControl
{
    public sealed class ImportData
    {
        public string fileName { get; set; } = null;

        public string description { get; set; } = null;

        public string sheetName { get; set; } = null;

        public RecordData[] records { get; set; } = null;
    }

    public sealed class RecordData
    {
        [Serializable]
        public sealed class Behavior
        {
            public float successRate { get; set; } = 0;

            public string actionType { get; set; } = null;

            public string actionParameters { get; set; } = null;

            public string targetType { get; set; } = null;

            public string targetParameters { get; set; } = null;

            public Condition[] conditions { get; set; } = null;
        }

        [Serializable]
        public sealed class Condition
        {
            public string type { get; set; } = null;

            public string parameters { get; set; } = null;

            public string connecter { get; set; } = null;
        }

        public Behavior behavior { get; set; } = null;
    }
}
