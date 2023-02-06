
namespace Extensions
{
    public static class GitUtility
    {
        /// <summary> 現在のブランチ名を取得 </summary>
        public static string GetBranchName(string workingDirectory, bool trimAsterisk = true)
        {
            if (string.IsNullOrEmpty(workingDirectory)){ return null; }
            
            var result = ExecuteGitProcess(workingDirectory, "branch --contains");

            if (IsInvalidResult(result)){ return null; }

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

            var result = ExecuteGitProcess(workingDirectory, "log --pretty=%H -n 1");

			if (IsInvalidResult(result)){ return null; }

			// 改行コードを削除.
			return result.Output.Replace("\r", "").Replace("\n", "");
		}

		/// <summary> 指定のブランチをチェックアウト </summary>
		public static bool Checkout(string workingDirectory, string branchName, bool force = true)
		{
			var result = ExecuteGitProcess(workingDirectory, $"checkout {branchName}" + (force ? " -f" : string.Empty));

			if (IsInvalidResult(result)){ return false; }
            
			return true;
		}

        /// <summary> 現在のブランチを最新にする </summary>
        public static bool Pull(string workingDirectory)
        {
            var result = ExecuteGitProcess(workingDirectory, "pull");

            if (IsInvalidResult(result)){ return false; }
            
            return true;
        }

        /// <summary> ワークスペースを破棄する </summary>
        public static bool Clean(string workingDirectory)
        {
            ProcessExecute.Result result = null;

            result = ExecuteGitProcess(workingDirectory, "checkout -f");

            if (IsInvalidResult(result)){ return false; }

            result = ExecuteGitProcess(workingDirectory, "clean -fd");

            if (IsInvalidResult(result)){ return false; }
            
            return true;
        }

        private static ProcessExecute.Result ExecuteGitProcess(string workingDirectory, string command)
        {
            var processExecute = new ProcessExecute("git", command)
            {
                WorkingDirectory = workingDirectory,
            };
            
            return processExecute.Start();
        }

        private static bool IsInvalidResult(ProcessExecute.Result result)
        {
            return result == null || 
                   string.IsNullOrEmpty(result.Output) || 
                   result.Output.StartsWith("error");
        }
    }
}
