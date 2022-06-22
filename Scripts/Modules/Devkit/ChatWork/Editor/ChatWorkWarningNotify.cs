
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.AssemblyCompilation;

namespace Modules.Devkit.ChatWork
{
    public sealed class ChatWorkWarningNotify : AssemblyCompilation<ChatWorkWarningNotify>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		[DidReloadScripts]
        public static void OnDidReloadScripts()
        {
            Instance.OnAssemblyReload();
        }

        protected override void OnCompileFinished(CompileResult[] results)
        {
            var hasWarning = false;

            var builder = new StringBuilder();

            var branch = GitUtility.GetBranchName(Application.dataPath);

            builder.AppendFormat("(lightbulb) Warning Notify ({0} : {1}) (lightbulb)", EditorUserBuildSettings.activeBuildTarget, branch).AppendLine();
            builder.AppendLine();

            foreach (var item in results)
            {
                var contents = item.Messages.Where(x => x.type == CompilerMessageType.Warning).ToArray();

                if (contents.Any())
                {
                    hasWarning = true;

                    builder.AppendFormat("Assembly : {0}", item.Assembly).AppendLine();
                    builder.AppendLine();

                    foreach (var content in contents)
                    {
                        var message = content.message.Replace("): warning ", ")\n");

                        builder.AppendFormat("{0}", message).AppendLine();
                        builder.AppendLine();
                    }
                }
            }

            using (new DisableStackTraceScope())
            {
                Debug.Log("CompileFinished.");
            }

            if (hasWarning)
            {
                PostMessage(builder.ToString()).Forget();
            }
            else
            {
                Exit(0).Forget();
            }
        }

        private async UniTask PostMessage(string message)
        {
            var exitCode = 0;

            var config = ChatWorkNotifyConfig.Instance;

            if (config != null)
            {
                var chatWorkMessage = new ChatWorkMessage(config.ApiToken, config.RoomId);

                await chatWorkMessage.SendMessage(message);
            }
            else
            {
                exitCode = 1;
            }

            Exit(exitCode).Forget();
        }

        private async UniTask Exit(int exitCode)
        {
            if (!Application.isBatchMode) { return; }

            await UniTask.NextFrame();

            EditorApplication.Exit(exitCode);
        }
    }
}
