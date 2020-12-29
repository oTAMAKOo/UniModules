
using UnityEngine;
using System;
using UniRx;
using Constants;

namespace Modules.SceneManagement
{
    /// <summary>
    /// シーンに渡す引数.
    /// </summary>
    public interface ISceneArgument
    {
        /// <summary> シーン識別子 </summary>
        Scenes? Identifier { get; }
        /// <summary> 事前読み込みを行うシーン </summary>
        Scenes[] PreLoadScenes { get; }
        /// <summary> キャッシュ対象か </summary>
        bool Cache { get; }
    }

    public interface ISceneBase
    {
        /// <summary> シーン引数を設定 </summary>
        void SetArgument(ISceneArgument argument);

        /// <summary> シーンの初期化 </summary>
        IObservable<Unit> Initialize();

        /// <summary> シーンの準備 (通信、読み込みなど) </summary>
        IObservable<Unit> Prepare(bool isSceneBack = false);

        /// <summary> シーンの開始 </summary>
        void Enter(bool isSceneBack = false);

        /// <summary> シーンの終了 </summary>
        IObservable<Unit> Leave();

        /// <summary> シーンの再読み込み  </summary>
        void Reload();
    }

    public abstract class SceneBase : MonoBehaviour, ISceneBase
    {
        /// <summary> シーン引数を設定 </summary>
        public abstract void SetArgument(ISceneArgument argument);

        /// <summary> シーンの初期化 </summary>
        public abstract IObservable<Unit> Initialize();

        /// <summary> シーン準備処理 </summary>
        public abstract IObservable<Unit> Prepare(bool isSceneBack);

        /// <summary> シーン開始時処理 </summary>
        public abstract void Enter(bool isSceneBack);

        /// <summary> シーン離脱時処理 </summary>
        public abstract IObservable<Unit> Leave();

        /// <summary> シーン再読み込み時処理 </summary>
        public abstract void Reload();
    }
}
