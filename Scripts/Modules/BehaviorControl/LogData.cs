
using System.Collections.Generic;

namespace Modules.BehaviorControl
{
    public sealed class LogData
    {
        //----- params -----

        public sealed class Node
        {
            public string Type { get; set; }

            public string Parameter { get; set; }

            public bool? Result { get; set; }
        }
        
        public sealed class Element
        {
            public float Percentage { get; set; }

            public float Probability { get; set; }

            public Node ActionNode { get; set; }

            public Node TargetNode { get; set; }

            public Node[] ConditionNodes { get; set; }

            public ConditionConnecter[] Connecters { get; set; }
        }

        //----- field -----
        
        private List<Element> elements = null;

        //----- property -----

        public string ControllerName { get; private set; }

        public string BehaviorName { get; private set; }

        public IReadOnlyList<Element> Elements { get { return elements; } }

        //----- method -----

        public LogData(string controllerName, string behaviorName)
        {
            ControllerName = controllerName;
            BehaviorName = behaviorName;

            elements = new List<Element>();
        }

        public void AddElement(Element element)
        {
            elements.Add(element);
        }
    }
}
