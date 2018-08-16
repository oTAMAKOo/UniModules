﻿﻿
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.QuadTree
{
    public interface IQuadTreeObject
    {
        Rect GetBounds();
    }

    public class QuadTree<T> where T : IQuadTreeObject
    {
        //----- params -----

        //----- field -----

        private int level = 0;
        private Rect bounds = new Rect();
        private List<T> objects = new List<T>();
        private QuadTree<T>[] nodes = new QuadTree<T>[4];
        private int maxLevel = 0;

        //----- property -----

        //----- method -----

        public void Initialize(int maxLevel, Rect bounds)
        {
            this.maxLevel = maxLevel;
            this.bounds = bounds;

            Split();
        }

        public void Insert(T instance)
        {
            if (Contains(instance)) { return; }

            var pRect = instance.GetBounds();

            if (nodes[0] != null)
            {
                List<int> indexes = GetIndexes(pRect);
                for (int ii = 0; ii < indexes.Count; ii++)
                {
                    int index = indexes[ii];
                    if (index != -1)
                    {
                        nodes[index].Insert(instance);
                        return;
                    }
                }

            }

            objects.Add(instance);

            if (level < maxLevel)
            {
                if (nodes[0] == null)
                {
                    Split();
                }

                var i = 0;

                while (i < objects.Count)
                {
                    var item = objects[i];
                    var indexes = GetIndexes(item.GetBounds());

                    for (int ii = 0; ii < indexes.Count; ++ii)
                    {
                        var index = indexes[ii];

                        if (index != -1)
                        {
                            nodes[index].Insert(item);
                            objects.Remove(item);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
        }

        public void Remove(T instance)
        {
            if(objects.Contains(instance))
            {
                objects.Remove(instance);
            }

            foreach (var node in nodes)
            {
                if (node != null)
                {
                    node.Remove(instance);
                }
            }
        }

        public bool Contains(T instance)
        {
            var result = false;

            result |= objects.Contains(instance);

            foreach (var node in nodes)
            {
                if (node != null)
                {
                    result |= node.Contains(instance);
                }
            }

            return result;
        }

        public T[] Get(Rect rect)
        {
            var list = new List<T>();

            return Retrieve(list, rect).ToArray();
        }

        public void Clear()
        {
            objects.Clear();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].Clear();
                    nodes[i] = null;
                }
            }
        }

        public void Update()
        {
            var allObjects = GetAllObjects();

            Clear();

            foreach(var item in allObjects)
            {
                Insert(item);
            }
        }

        public T[] GetAllObjects()
        {
            return objects.Concat(nodes.SelectMany(x => x.GetAllObjects())).ToArray();
        }

        private void Split()
        {
            var subWidth = (int)(bounds.width * 0.5f);
            var subHeight = (int)(bounds.height * 0.5f);
            var x = (int)bounds.x;
            var y = (int)bounds.y;

            nodes[0] = new QuadTree<T>() { level = level + 1, maxLevel = maxLevel, bounds = new Rect(x + subWidth, y, subWidth, subHeight) };
            nodes[1] = new QuadTree<T>() { level = level + 1, maxLevel = maxLevel, bounds = new Rect(x, y, subWidth, subHeight) };
            nodes[2] = new QuadTree<T>() { level = level + 1, maxLevel = maxLevel, bounds = new Rect(x, y + subHeight, subWidth, subHeight) };
            nodes[3] = new QuadTree<T>() { level = level + 1, maxLevel = maxLevel, bounds = new Rect(x + subWidth, y + subHeight, subWidth, subHeight) };
        }

        private List<int> GetIndexes(Rect pRect)
        {
            var indexes = new List<int>();

            var verticalMidpoint = bounds.x + (bounds.width / 2);
            var horizontalMidpoint = bounds.y + (bounds.height / 2);

            var topQuadrant = pRect.y >= horizontalMidpoint;
            var bottomQuadrant = (pRect.y - pRect.height) <= horizontalMidpoint;
            var topAndBottomQuadrant = pRect.y + pRect.height + 1 >= horizontalMidpoint && pRect.y + 1 <= horizontalMidpoint;

            if (topAndBottomQuadrant)
            {
                topQuadrant = false;
                bottomQuadrant = false;
            }

            // Check if object is in left and right quad
            if (pRect.x + pRect.width + 1 >= verticalMidpoint && pRect.x - 1 <= verticalMidpoint)
            {
                if (topQuadrant)
                {
                    indexes.Add(2);
                    indexes.Add(3);
                }
                else if (bottomQuadrant)
                {
                    indexes.Add(0);
                    indexes.Add(1);
                }
                else if (topAndBottomQuadrant)
                {
                    indexes.Add(0);
                    indexes.Add(1);
                    indexes.Add(2);
                    indexes.Add(3);
                }
            }

            // Check if object is in just right quad
            else if (pRect.x + 1 >= verticalMidpoint)
            {
                if (topQuadrant)
                {
                    indexes.Add(3);
                }
                else if (bottomQuadrant)
                {
                    indexes.Add(0);
                }
                else if (topAndBottomQuadrant)
                {
                    indexes.Add(3);
                    indexes.Add(0);
                }
            }
            // Check if object is in just left quad
            else if (pRect.x - pRect.width <= verticalMidpoint)
            {
                if (topQuadrant)
                {
                    indexes.Add(2);
                }
                else if (bottomQuadrant)
                {
                    indexes.Add(1);
                }
                else if (topAndBottomQuadrant)
                {
                    indexes.Add(2);
                    indexes.Add(1);
                }
            }
            else
            {
                indexes.Add(-1);
            }

            return indexes;
        }

        private List<T> Retrieve(List<T> retrievedList, Rect pRect)
        {
            List<int> indexes = GetIndexes(pRect);
            for (int ii = 0; ii < indexes.Count; ii++)
            {
                int index = indexes[ii];

                if (index != -1 && nodes[0] != null)
                {
                    nodes[index].Retrieve(retrievedList, pRect);
                }

                foreach (var obj in objects)
                {
                    if (!retrievedList.Contains(obj))
                    {
                        retrievedList.AddRange(objects);
                    }
                }
            }

            return retrievedList;
        }

        public void DrawDebug()
        {
            if(level == 0)
            {
                bounds.DrawGizmos();
            }

            foreach (var node in nodes)
            {
                if (node == null) { continue; }

                if (0 < node.objects.Count)
                {
                    node.bounds.DrawGizmos();
                }

                node.DrawDebug();
            }
        }
    }
}