
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.AssetBundles
{
    public sealed class AssetBundleDependencies
    {
        //----- params -----

        //----- field -----

        // 依存関係.
        private Dictionary<string, string[]> dependenciesTable = null;

        //----- property -----

        //----- method -----

        public AssetBundleDependencies()
        {
            dependenciesTable = new Dictionary<string, string[]>();
        }

        public void SetDependencies(string assetBundleName, string[] dependencies)
        {
            if (string.IsNullOrEmpty(assetBundleName)){ return; }

            if (dependencies == null){ return; }

            var list = dependenciesTable.GetValueOrDefault(assetBundleName);

            if (list != null){ return; }

            dependenciesTable[assetBundleName] = dependencies.Where(y => y != assetBundleName).ToArray();
        }

        /// <summary> 依存関係にあるアセット一覧取得. </summary>
        public string[] GetDependencies(string assetBundleName)
        {
            return dependenciesTable.GetValueOrDefault(assetBundleName);
        }

        /// <summary> 依存関係にあるアセット一覧取得. </summary>
        public string[] GetAllDependencies(string assetBundleName)
        {
            // 既に登録済みの場合はそこから取得.
            var dependents = dependenciesTable.GetValueOrDefault(assetBundleName);

            if (dependents == null)
            {
                // 依存アセット一覧を再帰で取得.
                dependents = GetAllDependenciesInternal(assetBundleName).ToArray();

                // 登録.
                if (dependents.Any())
                {
                    dependenciesTable.Add(assetBundleName, dependents);
                }
            }

            return dependents;
        }

        private IEnumerable<string> GetAllDependenciesInternal(string fileName, HashSet<string> dependents = null)
        {
            var targets = dependenciesTable.GetValueOrDefault(fileName, new string[0]);

            if (targets.IsEmpty()) { return new string[0]; }

            if (dependents == null)
            {
                dependents = new HashSet<string>();
            }

            foreach (var target in targets)
            {
                // 既に列挙済みの場合は処理しない.
                if (dependents.Contains(target)) { continue; }

                dependents.Add(target);

                // 依存先の依存先を取得.
                var internalDependents = GetAllDependenciesInternal(target, dependents);

                foreach (var internalDependent in internalDependents)
                {
                    // 既に列挙済みの場合は追加しない.
                    dependents.Add(internalDependent);
                }
            }

            return dependents;
        }

        public void Clear()
        {
            dependenciesTable.Clear();
        }
    }
}