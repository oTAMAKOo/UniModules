
using System;

namespace Extensions
{
    public static class GitUtility
    {
        /// <summary> 現在のブランチ名を取得 </summary>
        public static string GetBranchName(string workingDirectory, bool trimAsterisk = true)
        {
            if (string.IsNullOrEmpty(workingDirectory)){ return null; }
            
            var result = ExecuteGitProcess(workingDirectory, "branch --contains");

            CheckResult(result);

            if (string.IsNullOrEmpty(result.Output))
            {
                throw new Exception("result.Output is empty.");
            }

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

            CheckResult(result);

            if (string.IsNullOrEmpty(result.Output))
            {
                throw new Exception("result.Output is empty.");
            }

			// 改行コードを削除.
			return result.Output.Replace("\r", "").Replace("\n", "");
		}

		public static void Sync(string workingDirectory, string branchName)
		{
			Clean(workingDirectory);

			Fetch(workingDirectory);

			Reset(workingDirectory);

			Checkout(workingDirectory, branchName);

			RemoveLocalBranch(workingDirectory, branchName);

			Fetch(workingDirectory);

			Clean(workingDirectory);

			Pull(workingDirectory);
		}

		/// <summary> 指定のブランチをチェックアウト </summary>
		public static void Checkout(string workingDirectory, string branchName, bool force = true)
		{
			var command = $"checkout {branchName}";

			if (!IsLocalBranch(workingDirectory, branchName))
			{
				command = $"checkout -b {branchName} --track origin/{branchName}";
			}

			var result = ExecuteGitProcess(workingDirectory, command + (force ? " -f" : string.Empty));

			CheckResult(result);

			var currentBranch = GetBranchName(workingDirectory);

			CheckResult(result);

			if (!currentBranch.Contains(branchName))
			{
				throw new Exception($"checkout failed :\nCurrent: {currentBranch}\nTarget: {branchName}");
			}
		}


        /// <summary> 現在のブランチを最新にする </summary>
        public static void Pull(string workingDirectory)
        {
            var result = ExecuteGitProcess(workingDirectory, "pull");

            CheckResult(result);
        }
		
        public static void Clean(string workingDirectory)
        {
            ProcessExecute.Result result = null;

            result = ExecuteGitProcess(workingDirectory, "checkout -f");

            CheckResult(result);

            result = ExecuteGitProcess(workingDirectory, "clean -fd");

            CheckResult(result);
        }

		public static void Fetch(string workingDirectory)
		{
			var result = ExecuteGitProcess(workingDirectory, "fetch --prune origin");

			CheckResult(result);
		}
        
		public static void Reset(string workingDirectory)
		{
			var result = ExecuteGitProcess(workingDirectory, "reset --hard HEAD --");

			CheckResult(result);
		}

		private static bool IsLocalBranch(string workingDirectory, string branchName)
		{
			var result = ExecuteGitProcess(workingDirectory, "branch");

			CheckResult(result);

			if (string.IsNullOrEmpty(result.Output))
			{
				throw new Exception("result.Output is empty.");
			}

			var branchs = result.Output.Replace("\r\n", "\n").Split(new[] { "\n" }, StringSplitOptions.None);

			foreach (var branch in branchs)
			{
				var b = branch.TrimStart('*', ' ');

				if (b.Equals(branchName))
				{
					return true;
				}
			}

			return false;
		}

		private static void RemoveLocalBranch(string workingDirectory, string branchName)
		{
			var result = ExecuteGitProcess(workingDirectory, "branch");

			CheckResult(result);

			if (string.IsNullOrEmpty(result.Output))
			{
				throw new Exception("result.Output is empty.");
			}

			var branchs = result.Output.Replace("\r\n", "\n").Split(new[] { "\n" }, StringSplitOptions.None);

			foreach (var branch in branchs)
			{
				var b = branch.TrimStart('*', ' ');

				if (!b.Equals(branchName))
				{
					result = ExecuteGitProcess(workingDirectory, $"branch -D {b}");

					CheckResult(result);
				}
			}

		}

        private static ProcessExecute.Result ExecuteGitProcess(string workingDirectory, string command)
        {
            var processExecute = new ProcessExecute("git", command)
            {
                WorkingDirectory = workingDirectory,
            };
            
            return processExecute.Start();
        }

        private static void CheckResult(ProcessExecute.Result result)
        {
            if (result == null)
            {
                throw new Exception("result is null.");
            }

            if (result.Output.StartsWith("error"))
            {
                throw new Exception(result.Output);
            }
        }
    }
}
