
using System.Threading.Tasks;

namespace Extensions
{
    public static class GitUtility
    {
		/// <summary> 現在のコミットハッシュを取得 </summary>
		public static async Task<string> GetCommitHash(string workingDirectory)
		{
			if (string.IsNullOrEmpty(workingDirectory)){ return null; }

			var processExecute = new ProcessExecute("git", "log --pretty=%H -n 1")
			{
				WorkingDirectory = workingDirectory,
			};

			var result = await processExecute.StartAsync();

			if (string.IsNullOrEmpty(result.Output)){ return null; }

			// 改行コードを削除.
			return result.Output.Replace("\r", "").Replace("\n", "");
		}
	}
}