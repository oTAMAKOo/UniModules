
using UnityEngine;
using Extensions;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using CriWare;

#endif

namespace Modules.CriWare
{
    public static class CriWareConsoleEvent
    {
        public static readonly string Name = "CRI";
        public static readonly Color Color = new Color(135, 206, 235);
    }

    public sealed class CriWareObject : SingletonMonoBehaviour<CriWareObject>
    {
        //----- params -----

        private const string CriWareManageObjectName = "CRIWARE";

        //----- field -----

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

        [SerializeField]
        private Prefab initializerPrefab = null;

        #endif

        //----- property -----

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

        public CriWareInitializer Initializer { get; private set; }
        public CriWareCustomErrorHandler ErrorHandler { get; private set; }

        #endif

        //----- method -----

        public void Initialize(string cryptoKey)
        {
            #if ENABLE_CRIWARE_SOFDEC

            if (!CriManaPlugin.IsLibraryInitialized())
            {
                #if CRIWARE_VP9_SUPPORT

                // VP9初期化.

                if (CriManaVp9.SupportCurrentPlatform())
                {
                    CriManaVp9.SetupVp9Decoder();
                }

                #endif
            }

            #endif

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

            // CRIの管理GameObject名にリネーム.
            gameObject.transform.name = CriWareManageObjectName;

            // CriAtomServer → CriAtomの順で初期化しないとCriAtomにCriAtomServerを生成されてしまう.
            CriAtomServer.CreateInstance();

            if (Initializer == null)
            {
                // Acfを管理する為自動初期化を無効化.
                var source = initializerPrefab.Source;
                var initializerSource = UnityUtility.GetComponent<CriWareInitializer>(source);
                initializerSource.dontInitializeOnAwake = true;

                // 生成.
                Initializer = initializerPrefab.Instantiate<CriWareInitializer>();
                Initializer.transform.name = "Initializer";

                //========================================================
                // ライブラリ初期化(この処理完了後にCRIが有効化される).
                //========================================================

                // 手動管理する為無効化.
                Initializer.dontDestroyOnLoad = false;

                if (!string.IsNullOrEmpty(cryptoKey))
                {
                    // ※ ADX2-LEは自動で暗号化処理が掛かるのでDecrypterConfig自体が存在しない.

                    #if !ENABLE_CRIWARE_ADX_LE

                    // 認証キー.
                    Initializer.DecrypterConfig.key = cryptoKey;

                    #endif
                }

                // インゲームプレビュー無効化.
                if (!Debug.isDebugBuild)
                {
                    Initializer.atomConfig.usesInGamePreview = false;
                }

                // ファイルハンドルの使用数を節約する為、ファイルアクセスが行われるタイミングでのみハンドルを利用するよう変更.
                // ※ 逐次オープンとなるためファイルアクセスの性能は低下する.
                Initializer.fileSystemConfig.minimizeFileDescriptorUsage = true;

                // 初期化実行.
                Initializer.Initialize();
            }

            if (ErrorHandler == null)
            {
                ErrorHandler = UnityUtility.CreateGameObject<CriWareCustomErrorHandler>(gameObject, "ErrorHandler");

                ErrorHandler.Initialize();
            }

            #if ENABLE_CRIWARE_FILESYSTEM

            // CriFsServerはライブラリの初期化後に生成.
            CriFsServer.CreateInstance();

            #endif

            // CriAtomはライブラリの初期化後に生成.
            var criAtom = gameObject.AddComponent<CriAtom>();

            // CriAtomのSetupが隠蔽されているので手動で初期化する.
            criAtom.acfFile = Initializer.atomConfig.acfFileName;

            if (!string.IsNullOrEmpty(criAtom.acfFile))
            {
                var streamingAssetsPath = string.Empty; 

                #if UNITY_EDITOR

                streamingAssetsPath = UnityPathUtility.StreamingAssetsPath;

                #else

                streamingAssetsPath = Common.streamingAssetsPath;

                #endif

                var acfPath = PathUtility.Combine(streamingAssetsPath, criAtom.acfFile);

                acfPath += CriAssetDefinition.AcfExtension;

                CriAtomEx.RegisterAcf(null, acfPath);
            }

            #endif
        }
    }
}
