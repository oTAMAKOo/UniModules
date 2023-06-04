
using UnityEngine;
using UnityEditor;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.ChatWork;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    public sealed class TextureSizeChatWorkNotify
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public async UniTask SendNotifyMessage()
        {
            var validateTextureSize = new ValidateTextureSize();

            var hasWarning = false;

            var branch = GitUtility.GetBranchName(Application.dataPath);

            var result = validateTextureSize.Validate();

            var builder = new StringBuilder();

            builder.Append("[info]");
            builder.Append($"[title]TextureSize Warning ({branch})[/title]");

            foreach (var item in result)
            {
                if (item.violationTextures.IsEmpty()) { continue; }
                
                var folderPath = AssetDatabase.GUIDToAssetPath(item.validateData.folderGuid);

                builder.AppendLine($"Folder : {folderPath} ({item.validateData.width}x{item.validateData.heigth})");

                builder.AppendLine("Textures:");

                foreach (var texture in item.violationTextures)
                {
                    var texturePath = AssetDatabase.GetAssetPath(texture);

                    builder.AppendLine($"{texturePath} ({texture.width}x{texture.height})");
                }

                builder.AppendLine();

                hasWarning = true;
            }

            
            builder.Append("[/info]");

            if (hasWarning)
            {
                await PostMessage(builder.ToString());
            }
            
            await Exit(0);
        }

        private async UniTask PostMessage(string message)
        {
            var exitCode = 0;

            var config = TextureSizeChatWorkNotifyConfig.Instance;

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