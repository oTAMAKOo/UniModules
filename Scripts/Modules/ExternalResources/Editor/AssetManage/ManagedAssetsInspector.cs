﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    [CustomEditor(typeof(ManagedAssets), true)]
    public sealed class ManagedAssetsInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private ManagedAssets instance = null;

        private IGrouping<string, ManageInfo>[] manageInfoGroup = null;
        
        private Dictionary<string, Object> assetCacheByGuid = null;

        private Vector2 scrollPosition = Vector2.zero;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        private void Initialize()
        {
            if (initialized){ return; }
            
            assetCacheByGuid = new Dictionary<string, Object>();

            manageInfoGroup = instance.GetAllInfos()
                .GroupBy(x => x.category)
                .OrderBy(x => x.Key, new NaturalComparer())
                .ToArray();

            initialized = true;
        }

        public override void OnInspectorGUI()
        {
            instance = target as ManagedAssets;

            Initialize();

            EditorGUILayout.Separator();

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(700f)))
            {
                foreach (var group in manageInfoGroup)
                {
                    EditorLayoutTools.Title(group.Key, new Color(0.2f, 0f, 1f, 1f));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(8f);

                        using (new EditorGUILayout.VerticalScope())
                        {
                            foreach (var manageInfo in group)
                            {
                                DrawManageInfoGUI(manageInfo);
                            }  
                        }

                        GUILayout.Space(8f);
                    }
                }
                
                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        private void DrawManageInfoGUI(ManageInfo manageInfo)
        {
            if (manageInfo == null) { return; }

            var guid = manageInfo.guid;

            Object asset;

            if (!assetCacheByGuid.ContainsKey(guid))
            {
                asset = UnityEditorUtility.FindMainAsset(guid);

                assetCacheByGuid[guid] = asset;
            }
            else
            {
                asset = assetCacheByGuid.GetValueOrDefault(guid);
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            EditorLayoutTools.ContentTitle(assetPath, new Color(0.2f, 1f, 0f, 1f));

            using (new ContentsScope())
            {
                using (new DisableScope(true))
                {
                    EditorGUILayout.ObjectField("Asset", asset, typeof(Object), false);

                    EditorGUILayout.TextField("Category", manageInfo.category);

                    if (!string.IsNullOrEmpty(manageInfo.tag))
                    {
                        EditorGUILayout.TextField("Tag", manageInfo.tag);
                    }

                    if (!string.IsNullOrEmpty(manageInfo.comment))
                    {
                        EditorGUILayout.TextField("Comment", manageInfo.comment);
                    }

                    if (manageInfo.isAssetBundle)
                    {
                        EditorGUILayout.EnumPopup("NamingRule", manageInfo.assetBundleNamingRule);

                        if (!string.IsNullOrEmpty(manageInfo.assetBundleNameStr))
                        {
                            EditorGUILayout.TextField("NameStr", manageInfo.assetBundleNameStr);
                        }
                    }
                }
            }

            EditorGUILayout.Space(3f);
        }
    }
}
