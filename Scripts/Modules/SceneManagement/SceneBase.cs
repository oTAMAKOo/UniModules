using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
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
		/// <summary> 引数型を取得 </summary>
		Type GetArgumentType();

		/// <summary> 引数を設定 </summary>
		void SetArgument(ISceneArgument argument);

		/// <summary> 初期化 </summary>
		UniTask Initialize();

		/// <summary> 準備 (通信、読み込みなど) </summary>
		UniTask Prepare(bool isSceneBack = false);

		/// <summary> 開始 </summary>
		void Enter(bool isSceneBack = false);

		/// <summary> 終了 </summary>
		UniTask Leave();

		/// <summary> 再読み込み  </summary>
		void Reload();
	}

	public abstract class SceneBase : MonoBehaviour, ISceneBase
	{
		/// <summary> 引数型を取得 </summary>
		public abstract Type GetArgumentType();

		/// <summary> 引数を設定 </summary>
		public abstract void SetArgument(ISceneArgument argument);

		/// <summary> 初期化 </summary>
		public virtual UniTask Initialize() { return UniTask.CompletedTask; }

		/// <summary> 準備処理 </summary>
		public virtual UniTask Prepare(bool isSceneBack) { return UniTask.CompletedTask; }

		/// <summary> 開始時処理 </summary>
		public virtual void Enter(bool isSceneBack) { }

		/// <summary> 離脱時処理 </summary>
		public virtual UniTask Leave(){ return UniTask.CompletedTask; }

		/// <summary> 再読み込み時処理 </summary>
		public virtual void Reload(){ }
	}
}
