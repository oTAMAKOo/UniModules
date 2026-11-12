
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System;

namespace Modules.Devkit.Markdown
{
    /// <summary>
    /// TextAssetのインスペクタ.
    /// 整形表示の対象のファイルはMarkdownとして表示し、対象外のファイルは標準のインスペクタを表示する.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TextAsset))]
    public sealed class MarkdownAssetInspector : UnityEditor.Editor
    {
        //----- params -----

        private const string DefaultInspectorTypeName = "UnityEditor.TextAssetInspector";

        private const string ToolbarClassName = "md-toolbar";

        //----- field -----

        private UnityEditor.Editor defaultEditor = null;

        //----- property -----

        //----- method -----

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // 生成した標準のインスペクタは、表示が外れた時点で破棄する.
            root.RegisterCallback<DetachFromPanelEvent>(x => DestroyDefaultEditor());

            var assetPath = AssetDatabase.GetAssetPath(target);

            var rule = targets.Length == 1 ? MarkdownViewRules.Find(assetPath) : null;

            if (rule == null)
            {
                root.Add(CreateDefaultInspector());

                return root;
            }

            var textAsset = target as TextAsset;

            var markdownView = new MarkdownView();

            markdownView.SetStyleSheet(MarkdownViewRules.LoadStyleSheet(rule));
            markdownView.SetMarkdown(textAsset.text, assetPath);

            VisualElement sourceView = null;

            // 原文表示の切り替え.

            var toolbar = new Toolbar();

            toolbar.AddToClassList(ToolbarClassName);

            var spacer = new ToolbarSpacer();

            spacer.style.flexGrow = 1;

            var sourceToggle = new ToolbarToggle() { text = "Source" };

            sourceToggle.RegisterValueChangedCallback(e =>
            {
                if (e.newValue && sourceView == null)
                {
                    sourceView = CreateDefaultInspector();

                    root.Add(sourceView);
                }

                markdownView.style.display = e.newValue ? DisplayStyle.None : DisplayStyle.Flex;

                if (sourceView != null)
                {
                    sourceView.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                }
            });

            // 別ウィンドウで開く.

            var windowButton = new ToolbarButton(() => MarkdownWindow.Open(textAsset)) { text = "Open Window" };

            // 左端のボタンは、標準の負のマージンで表示領域の外へ出ないようにする.
            windowButton.style.marginLeft = 0;

            toolbar.Add(windowButton);
            toolbar.Add(spacer);
            toolbar.Add(sourceToggle);

            root.Add(toolbar);
            root.Add(markdownView);

            // インポートされたアセットのインスペクタは無効状態で表示されるため、リンクを押せるように有効化する.
            root.RegisterCallback<AttachToPanelEvent>(x => EnableHierarchy(root));

            // インスペクタの余白を打ち消して、インスペクタの幅いっぱいに表示する.
            root.RegisterCallback<GeometryChangedEvent>(x => FitToInspector(root));

            // アセットのヘッダーを表示しない. ヘッダーは他のインスペクタでも使われるため、表示が外れた時点で元に戻す.
            root.RegisterCallback<AttachToPanelEvent>(x => SetHeaderVisible(root, false));
            root.RegisterCallback<DetachFromPanelEvent>(x => SetHeaderVisible(root, true));

            return root;
        }

        /// <summary> Unity標準のTextAssetのインスペクタを生成する </summary>
        private VisualElement CreateDefaultInspector()
        {
            DestroyDefaultEditor();

            var type = typeof(UnityEditor.Editor).Assembly.GetType(DefaultInspectorTypeName);

            if (type == null)
            {
                return new HelpBox("Default inspector type not found.", HelpBoxMessageType.Warning);
            }

            defaultEditor = CreateEditor(targets, type);

            var element = defaultEditor.CreateInspectorGUI();

            if (element != null){ return element; }

            var editor = defaultEditor;

            return new IMGUIContainer(() =>
            {
                if (editor != null)
                {
                    editor.OnInspectorGUI();
                }
            });
        }

        private void DestroyDefaultEditor()
        {
            if (defaultEditor == null){ return; }

            DestroyImmediate(defaultEditor);

            defaultEditor = null;
        }

        /// <summary> 親を辿って無効状態を解除する </summary>
        private static void EnableHierarchy(VisualElement element)
        {
            var inspectorElement = FindInspectorElement(element);

            if (inspectorElement == null){ return; }

            inspectorElement.SetEnabled(true);
        }

        /// <summary> 親のインスペクタの余白と同じ幅だけ外側へ広げる </summary>
        private static void FitToInspector(VisualElement element)
        {
            var inspectorElement = FindInspectorElement(element);

            if (inspectorElement == null){ return; }

            var style = inspectorElement.resolvedStyle;

            element.style.marginLeft = -style.paddingLeft;
            element.style.marginRight = -style.paddingRight;
            element.style.marginTop = -style.paddingTop;
            element.style.marginBottom = -style.paddingBottom;
        }

        /// <summary> 親のインスペクタより前に並ぶ要素(アセットのヘッダー)の表示を切り替える </summary>
        private static void SetHeaderVisible(VisualElement element, bool visible)
        {
            var inspectorElement = FindInspectorElement(element);

            if (inspectorElement == null){ return; }

            var container = inspectorElement.parent;

            if (container == null){ return; }

            foreach (var child in container.Children())
            {
                if (child == inspectorElement){ break; }

                // ヘッダーの描画処理はインスペクタ本体の表示状態を更新するため、非表示にはせず高さを無くして隠す.

                if (visible)
                {
                    child.style.height = StyleKeyword.Null;
                    child.style.overflow = StyleKeyword.Null;
                }
                else
                {
                    child.style.height = 0f;
                    child.style.overflow = Overflow.Hidden;
                }
            }
        }

        private static VisualElement FindInspectorElement(VisualElement element)
        {
            var current = element;

            while (current != null)
            {
                if (current is InspectorElement){ return current; }

                current = current.parent;
            }

            return null;
        }
    }
}
