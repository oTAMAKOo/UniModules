
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Callbacks;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.AssemblyCompilation;
using Modules.Devkit.ChatWork;

namespace Modules.Devkit.ValidateAsset.UnityWarning
{
    public sealed class UnityWarningChatWorkNotify : AssemblyCompilation<UnityWarningChatWorkNotify>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        public static void OnDidReloadScripts()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OnDidReloadScripts;
                return;
            }

            EditorApplication.delayCall += OnAfterDidReloadScripts;
        }

        private static void OnAfterDidReloadScripts()
        {
            Instance.OnAssemblyReload();
        }

        public void SendNotifyMessage()
        {
            RequestCompile();
        }

        protected override void OnCompileFinished(CompileResult[] results)
        {
            var hasWarning = false;

            var builder = new StringBuilder();

            var branch = GitUtility.GetBranchName(UnityPathUtility.DataPath);
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;

            builder.Append("[info]");
            builder.Append($"[title]Warning {buildTarget} ({branch})[/title]");

            foreach (var item in results)
            {
                var contents = item.Messages.Where(x => x.type == CompilerMessageType.Warning).ToArray();

                if (contents.Any())
                {
                    hasWarning = true;

                    builder.Append(item.Assembly);
                    builder.Append("[code]");

                    foreach (var content in contents)
                    {
                        var warningText = content.message.Replace("): warning ", ")\n");

                        builder.AppendFormat("{0}", warningText).AppendLine();
                        builder.AppendLine();
                    }

                    builder.Append("[/code]");
                }
            }

            builder.Append("[/info]");

            var message = hasWarning ? builder.ToString() : string.Empty;

            PostMessage(message).Forget();
        }

        private async UniTask PostMessage(string message)
        {
            var exitCode = 0;

            if (!string.IsNullOrEmpty(message))
            {
                var config = UnityWarningChatWorkNotifyConfig.Instance;

                if (config != null)
                {
                    var setting = config.LoadSettingJson();

                    var chatWorkMessage = new ChatWorkMessage(setting.ApiToken, setting.RoomId);

                    await chatWorkMessage.SendMessage(message);
                }
                else
                {
                    exitCode = 1;
                }
            }

            Exit(exitCode).Forget();
        }

        private async UniTask Exit(int exitCode)
        {
            if (!Application.isBatchMode) { return; }

            await UniTask.Delay(1);

            EditorApplication.Exit(exitCode);
        }
    }
}
