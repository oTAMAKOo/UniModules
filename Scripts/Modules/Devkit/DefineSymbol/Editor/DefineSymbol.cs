
using UnityEditor;
using UnityEditor.Build;
using System.Linq;
using System.Collections.Generic;

namespace Modules.Devkit.DefineSymbol
{
    public static class DefineSymbol
    {
        //----- params -----

        //----- field -----

        private static List<string> defineSymbols = null;

        //----- property -----

        public static IReadOnlyList<string> Current
        {
            get
            {
                if (defineSymbols == null)
                {
                    Load();
                }

                return defineSymbols;
            }
        }

        //----- method -----

        /// <summary> 指定された要素で上書き </summary>
        public static void Set(IEnumerable<string> targets, bool save = false)
        {
            if (targets == null){ return; }

            defineSymbols = targets.ToList();

            if (save)
            {
                Save();
            }
        }

        /// <summary> 指定された要素を末尾に追加 </summary>
        public static void Add(string target, bool save = false)
        {
            if (Contains(target)){ return; }

            defineSymbols.Add(target);

            if (save)
            {
                Save();
            }
        }

        /// <summary> 指定されたインデックスの位置に要素を挿入 </summary>
        public static void Insert(int index, string target, bool save = false)
        {
            if (Contains(target)) { return; }

            defineSymbols.Insert(index, target);

            if (save)
            {
                Save();
            }
        }

        /// <summary> 指定された要素を削除 </summary>
        public static void Delete(string target, bool save = false)
        {
            if (string.IsNullOrEmpty(target)) { return; }

            defineSymbols = Current.Where(x => x != target).ToList();

            if (save)
            {
                Save();
            }
        }

        /// <summary> 指定された要素が含まれるか </summary>
        public static bool Contains(string target)
        {
            return Current.Any(x => x == target);
        }

        private static void Load()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            var defineSymbolStr = string.Empty;

            #if UNITY_6000_0_OR_NEWER

            defineSymbolStr = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            #else

            defineSymbolStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            #endif

            defineSymbols = defineSymbolStr.Split(';').ToList();
        }

        private static void Save()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            #if UNITY_6000_0_OR_NEWER

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup), defineSymbols.ToArray());

            #else

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols.ToArray());

            #endif

            AssetDatabase.SaveAssets();
        }
    }
}
