
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using UnityEditor;

namespace Modules.Devkit.Hierarchy
{
    public abstract class ItemContentDrawer
    {
        public abstract int Priority { get; }

        public abstract bool Enable { get; }

        public abstract void Initialize();

        public abstract Rect Draw(GameObject targetObject, Rect rect);
    }

    public abstract class HierarchyItemDrawer<TInstance> : Singleton<TInstance> where TInstance : HierarchyItemDrawer<TInstance>
    {
        //----- params -----

        //----- field -----

        private List<ItemContentDrawer> drawerList = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        protected override void OnCreate()
        {
            drawerList = new List<ItemContentDrawer>();

            Initialize();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            EditorApplication.hierarchyWindowItemOnGUI += OnDrawHierarchy;

            initialized = true;
        }
        
        private void OnDrawHierarchy(int instanceID, Rect rect)
        {
            var targetObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (targetObject == null){ return; }

            var padding = new Vector2(14f, 0f);

            var drawRect = new Rect(rect.xMax - padding.x, rect.yMin, rect.width, rect.height);

            foreach (var drawer in drawerList)
            {
                if (!drawer.Enable){ continue; }

                drawRect = drawer.Draw(targetObject, drawRect);
            }
        }

        public T GetDrawer<T>() where T : ItemContentDrawer
        {
            return drawerList.FirstOrDefault(x => x is T) as T;
        }

        public T AddDrawer<T>() where T : ItemContentDrawer
        {
            var drawer = Activator.CreateInstance<T>() as ItemContentDrawer;

            drawer.Initialize();

            drawerList.Add(drawer);

            drawerList = drawerList.OrderBy(x => x.Priority).ToList();

            return (T)drawer;
        }
    }
}
