
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using Extensions;
using Modules.Scene;

namespace Modules.Window
{
    public abstract class PopupStashManager<TInstance, TScenes, TPopupManager, TSceneManager> : Singleton<TInstance>
        where TInstance : PopupStashManager<TInstance, TScenes, TPopupManager, TSceneManager>
        where TScenes : struct, Enum
        where TPopupManager : PopupManager<TPopupManager>, new()
        where TSceneManager : SceneManagerBase<TSceneManager, TScenes>
    {
        //----- params -----

        private sealed class StashEntry
        {
            public TScenes OwnerScene { get; set; }

            public List<StashedWindow> Windows { get; set; }
        }

        private sealed class StashedWindow
        {
            public Window Window { get; set; }

            public bool WasActive { get; set; }
        }

        private const string ContainerName = "Popup (Stashed)";

        //----- field -----

        // 退避エントリのスタック（LIFOでネスト加算遷移に対応）.
        private Stack<StashEntry> stashStack = null;

        // シーン毎のStashRoot（子が空になったら削除）.
        private Dictionary<TScenes, GameObject> stashRoots = null;

        // DontDestroyOnLoad配下の共通コンテナ（全StashRootの親）.
        private GameObject container = null;

        private TSceneManager sceneManager = null;
        private TPopupManager popupManager = null;

        private bool initialized = false;

        //----- property -----

        /// <summary> 退避しているエントリがあるか </summary>
        public bool HasStash { get { return stashStack.Any(); } }

        /// <summary> 退避エントリ数 </summary>
        public int StashCount { get { return stashStack.Count; } }

        //----- method -----

        public void Initialize()
        {
            if (initialized){ return; }

            sceneManager = SceneManagerBase<TSceneManager, TScenes>.Instance;
            popupManager = PopupManager<TPopupManager>.Instance;

            // シーンLeave時にそのシーンをOwnerに持つStashを破棄.
            sceneManager.OnLeaveCompleteAsObservable()
                .Subscribe(x => OnSceneLeave(x))
                .AddTo(Disposable);

            stashStack = new Stack<StashEntry>();
            stashRoots = new Dictionary<TScenes, GameObject>();

            initialized = true;
        }

        /// <summary> 現在のアクティブシーンのScenePopupsを退避 </summary>
        public void Stash()
        {
            if (!initialized){ return; }

            var currentScene = sceneManager.Current;

            if (currentScene == null){ return; }

            if (!currentScene.Identifier.HasValue){ return; }

            var popups = popupManager.ScenePopups.ToArray();

            if (popups.Length == 0){ return; }

            var ownerScene = currentScene.Identifier.Value;

            var stashRoot = GetOrCreateStashRoot(ownerScene);

            if (UnityUtility.IsNull(stashRoot)){ return; }

            var windows = new List<StashedWindow>();

            foreach (var popup in popups)
            {
                if (UnityUtility.IsNull(popup)){ continue; }

                var stashed = new StashedWindow()
                {
                    Window = popup,
                    WasActive = popup.gameObject.activeSelf,
                };

                windows.Add(stashed);

                PopupManager<TPopupManager>.Unregister(popup);

                UnityUtility.SetParent(popup.gameObject, stashRoot);

                UnityUtility.SetActive(popup.gameObject, false);
            }

            var entry = new StashEntry()
            {
                OwnerScene = ownerScene,
                Windows = windows,
            };

            stashStack.Push(entry);
        }

        /// <summary> 直近の退避を復元 </summary>
        public void Restore()
        {
            if (stashStack.Count == 0){ return; }

            var entry = stashStack.Pop();

            foreach (var stashed in entry.Windows)
            {
                var window = stashed.Window;

                if (UnityUtility.IsNull(window)){ continue; }

                UnityUtility.SetActive(window.gameObject, stashed.WasActive);

                PopupManager<TPopupManager>.Open(window).Forget();
            }

            TryRemoveStashRoot(entry.OwnerScene);
        }

        /// <summary> 直近の退避を破棄（中身のWindowも削除） </summary>
        public void DiscardTop()
        {
            if (stashStack.Count == 0){ return; }

            var entry = stashStack.Pop();

            foreach (var stashed in entry.Windows)
            {
                var window = stashed.Window;

                if (UnityUtility.IsNull(window)){ continue; }

                UnityUtility.DeleteGameObject(window);
            }

            TryRemoveStashRoot(entry.OwnerScene);
        }

        /// <summary> 全退避を破棄 </summary>
        public void DiscardAll()
        {
            while (stashStack.Count > 0)
            {
                DiscardTop();
            }
        }

        /// <summary> 共通コンテナを取得（無ければ作成） </summary>
        private GameObject GetOrCreateContainer()
        {
            if (!UnityUtility.IsNull(container)){ return container; }

            container = UnityUtility.CreateEmptyGameObject(null, ContainerName);

            if (UnityUtility.IsNull(container)){ return null; }

            GameObject.DontDestroyOnLoad(container);

            return container;
        }

        /// <summary> シーン毎のStashRootを取得（無ければ作成） </summary>
        private GameObject GetOrCreateStashRoot(TScenes ownerScene)
        {
            if (stashRoots.TryGetValue(ownerScene, out var existing))
            {
                if (!UnityUtility.IsNull(existing)){ return existing; }

                stashRoots.Remove(ownerScene);
            }

            var parent = GetOrCreateContainer();

            if (UnityUtility.IsNull(parent)){ return null; }

            var name = $"Scene ({ownerScene})";

            var created = UnityUtility.CreateEmptyGameObject(parent, name);

            if (UnityUtility.IsNull(created)){ return null; }

            stashRoots[ownerScene] = created;

            return created;
        }

        /// <summary> 子が空ならStashRootを削除、全StashRootが無くなったらコンテナも削除 </summary>
        private void TryRemoveStashRoot(TScenes ownerScene)
        {
            if (!stashRoots.TryGetValue(ownerScene, out var root))
            {
                TryRemoveContainer();
                return;
            }

            if (UnityUtility.IsNull(root))
            {
                stashRoots.Remove(ownerScene);
                TryRemoveContainer();
                return;
            }

            if (root.transform.childCount > 0){ return; }

            UnityUtility.SafeDelete(root);

            stashRoots.Remove(ownerScene);

            TryRemoveContainer();
        }

        /// <summary> StashRootが全て無くなったらコンテナを削除 </summary>
        private void TryRemoveContainer()
        {
            if (stashRoots.Count > 0){ return; }

            if (UnityUtility.IsNull(container)){ return; }

            UnityUtility.SafeDelete(container);

            container = null;
        }

        /// <summary> シーンLeave時のクリーンアップ </summary>
        private void OnSceneLeave(SceneInstance<TScenes> sceneInstance)
        {
            if (sceneInstance == null){ return; }

            if (!sceneInstance.Identifier.HasValue){ return; }

            var leaveScene = sceneInstance.Identifier.Value;

            if (stashStack.Count == 0)
            {
                TryRemoveStashRoot(leaveScene);
                return;
            }

            // 該当シーンのStashを除外して積み直し.
            var array = stashStack.ToArray();

            stashStack.Clear();

            for (var i = array.Length - 1; i >= 0; i--)
            {
                var entry = array[i];

                if (EqualityComparer<TScenes>.Default.Equals(entry.OwnerScene, leaveScene))
                {
                    foreach (var stashed in entry.Windows)
                    {
                        var window = stashed.Window;

                        if (UnityUtility.IsNull(window)){ continue; }

                        UnityUtility.DeleteGameObject(window);
                    }

                    continue;
                }

                stashStack.Push(entry);
            }

            TryRemoveStashRoot(leaveScene);
        }
    }
}
