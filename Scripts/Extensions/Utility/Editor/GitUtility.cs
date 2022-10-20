
using Cysharp.Threading.Tasks;

namespace Extensions
{
    public static class GitUtility
    {
        /// <summary> 現在のブランチ名を取得 </summary>
        public static string GetBranchName(string workingDirectory, bool trimAsterisk = true)
        {
            if (string.IsNullOrEmpty(workingDirectory)){ return null; }

            var processExecute = new ProcessExecute("git", "branch --contains")
            {
                WorkingDirectory = workingDirectory,
            };

            var result = processExecute.Start();

            if (result == null){ return string.Empty; }

            if (string.IsNullOrEmpty(result.Output)){ return null; }

            // 改行コードを削除.
            var branchName = result.Output.Replace("\r", "").Replace("\n", "");

            if (trimAsterisk)
            {
                branchName = branchName.TrimStart('*', ' ');
            }

            return branchName;
        }

		/// <summary> 現在のコミットハッシュを取得 </summary>
		public static string GetCommitHash(string workingDirectory)
		{
			if (string.IsNullOrEmpty(workingDirectory)){ return null; }

			var processExecute = new ProcessExecute("git", "log --pretty=%H -n 1")
			{
				WorkingDirectory = workingDirectory,
			};

			var result = processExecute.Start();

			if (string.IsNullOrEmpty(result.Output)){ return null; }

			// 改行コードを削除.
			return result.Output.Replace("\r", "").Replace("\n", "");
		}

		/// <summary> 指定のブランチをチェックアウト </summary>
        public static async UniTask<bool> Checkout(string workingDirectory, string branchName)
        {
            var processExecute = new ProcessExecute("git", $"checkout {branchName}")
            {
                WorkingDirectory = workingDirectory,
            };

            await processExecute.StartAsync();

            var currentBranchName = GetBranchName(workingDirectory);

            return currentBranchName == branchName;
        }
	}
}
