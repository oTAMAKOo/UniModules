#if ENABLE_VSTU

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Modules.Devkit.VisualStudio
{
    partial class ProjectFile
    {
        /// <summary>
        /// 指定したファイル名が、Unity の C# プロジェクト ファイル (*.csproj) かどうかを判定.
        /// </summary>
        public static readonly Func<string, bool> IsUnityProject = name => name.ToLower().EndsWith(".csproj");

        /// <summary>
        /// 指定したファイル名が、UnityEditor の C# プロジェクト ファイル (*.Editor.csproj) かどうかを判定.
        /// </summary>
        public static readonly Func<string, bool> IsEditorProject = name => name.ToLower().EndsWith(".editor.csproj");

        /// <summary>
        /// 指定したファイル名が、UnityEditor の C# プロジェクト ファイル (*.Plugins.csproj) かどうかを判定.
        /// </summary>
        public static readonly Func<string, bool> IsPluginsProject = name => name.ToLower().EndsWith(".plugins.csproj");

        /// <summary>
        /// 指定したファイル名が、UnityEditor の C# プロジェクト ファイル (*.Editor.Plugins.csproj) かどうかを判定.
        /// </summary>
        public static readonly Func<string, bool> IsPluginsEditorProject = name => name.ToLower().EndsWith(".editor.plugins.csproj");

        /// <summary>
        /// 指定したファイル名が、Unity または UnityEditor の C# プロジェクト ファイルかどうかを判定.
        /// </summary>
        public static readonly Func<string, bool> IsUnityOrEditorOrPluginsProject = name =>
        {
            return IsUnityProject(name) || IsEditorProject(name) || IsPluginsProject(name) || IsPluginsEditorProject(name);
        };

        /// <summary>
        /// プロジェクト ファイル (.csproj) 内から 不要な.Lang への参照を削除するためのフック処理を実行.
        /// </summary>
        public static void ExcludeAnotherLanguageReference(ProjectFileGenerationArgs args)
        {
            var deleteTargets = new[]
            {
                "Boo.Lang",
                "UnityScript",
                "UnityScript.Lang",
            };

            var document = args.Content;

            XElement[] targetChilds = null;

            if (document.Root != null)
            {
                targetChilds = document.Root.Nodes()
                    .OfType<XElement>()
                    .Where(x => x.Name.LocalName == "ItemGroup")
                    .Where(x => x.FirstNode != null)
                    .Select(x => x.FirstNode)
                    .OfType<XElement>()
                    .ToArray();
            }

            if (targetChilds == null) { return; }

            if (targetChilds.Length == 0) { return; }

            foreach (var targetChild in targetChilds)
            {
                if (targetChild.Parent == null) { continue; }

                var targetNode = targetChild.Parent;

                var removeTargets = targetNode.Descendants()
                    .Where(x => x.Name.LocalName == "Reference")
                    .Where(x => x.Attribute("Include") != null)
                    .Where(x => !string.IsNullOrEmpty(x.Attribute("Include").Value))
                    .Where(x => deleteTargets.Any(y => x.Attribute("Include").Value.ToLower().Contains(y.ToLower())))
                    .ToArray();

                foreach (var removeTarget in removeTargets)
                {
                    removeTarget.Remove();
                }
            }
        }

        /// <summary>
        /// VisualStudio用のAnalyzerを参照に追加. 
        /// ※ analyzerIdにはNuGetのIDを指定.
        /// </summary>
        public static void IncludeAnalyzer(ProjectFileGenerationArgs args, string analyzerId)
        {
            // NuGetのpackages.configから取得.
            var currentDir = Directory.GetCurrentDirectory();
            var packagePath = Path.Combine(currentDir, "packages.config");
            
            if (!File.Exists(packagePath)) return;

            var packages = XDocument.Load(packagePath);

            var analyzerPackage = packages.Descendants("package")
                .FirstOrDefault(x => (string)x.Attribute("id") == analyzerId);

            if (analyzerPackage == null) return;

            var pathRoot =
                "packages\\" +
                (string)analyzerPackage.Attribute("id") + "." + (string)analyzerPackage.Attribute("version")
                + "\\analyzers\\dotnet\\cs";

            var ns = args.Content.Root.Name.Namespace;
            var dirPath = Path.Combine(currentDir, pathRoot);
            
            if (!Directory.Exists(dirPath)) return;
            var analyzers = Directory.GetFiles(Path.Combine(currentDir, pathRoot))
                .Select(x => new XElement(ns + "Analyzer", new XAttribute("Include", pathRoot + "\\" + Path.GetFileName(x))))
                .ToArray();

            if (analyzers.Length == 0) return;

            // 最後のItempGroupの下に追加.
            var lastGroup = args.Content.Descendants().Last(x => x.Name.LocalName == "ItemGroup");

            lastGroup.AddAfterSelf(new XElement(ns + "ItemGroup", analyzers));
        }
    }
}

#endif
