
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Modules.UI
{
    public abstract class GridVirtualScroll<T> : VirtualScroll<GridVirtualScroll<T>.GridElement> where T : class
    {
        //----- params -----

        public sealed class GridElement
        {
            public int StartIndex { get; private set; }

            public IReadOnlyList<T> Elements { get; private set; }

            public GridElement(int startIndex, T[] elements)
            {
                StartIndex = startIndex;
                Elements = elements.ToArray();
            }
        }

        //----- field -----

        [SerializeField]
        private int lineElementCount = 0;

        //----- property -----

        public int GridLineElementCount { get { return lineElementCount; } }
        
        //----- method -----

        public void SetLineElementCount(int count)
        {
            lineElementCount = count;
        }

        public void SetContents(T[] contents)
        {
            var elements = BuildElements(contents);

            SetContents(elements);
        }

        // ※ 基底クラスのSetContentsを隠蔽する.
        private new void SetContents(GridElement[] contents)
        {
            base.SetContents(contents);
        }

        // データを入れる順番を変更したい時はこの関数をoverrideする.
        protected virtual GridElement[] BuildElements(T[] contents)
        {
            var gridElements = new List<GridElement>();

            var elements = new List<T>();

            var startIndex = 0;
            var contentsIndex = 0;

            for (var i = 0; i < contents.Length; i++)
            {
                if (GridLineElementCount <= elements.Count)
                {
                    var girdElement = new GridElement(startIndex, elements.ToArray());

                    gridElements.Add(girdElement);

                    startIndex = contentsIndex;
                    elements.Clear();
                }

                var content = contents.ElementAtOrDefault(contentsIndex);

                if (content == null) { break; }

                elements.Add(content);

                contentsIndex++;
            }

            if (elements.Any())
            {
                var girdElement = new GridElement(startIndex, elements.ToArray());

                gridElements.Add(girdElement);
            }

            return gridElements.ToArray();
        }
    }
}
