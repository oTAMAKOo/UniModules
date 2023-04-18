
using System;
using System.Linq;
using System.Text;

namespace Extensions
{
    public static class LogUtility
    {
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

		public static void ChunkLog(string logs, string title, Action<string> outputCallback, int maxLine = 35)
		{
			if (outputCallback == null) { return; }

			var logBuilder = new StringBuilder();

			var logText = logs.FixLineEnd();

			var chunk = logText.Split('\n').Chunk(maxLine).ToArray();

			var length = chunk.Length;

			logBuilder.Append(title);

			for (var i = 0; i < length; i++)
			{
				var items = chunk[i];

				if (1 < chunk.Length)
				{
					logBuilder.Append($"[{i + 1}/{length}]");

					logBuilder.AppendLine();
				}

				foreach (var item in items)
				{
					logBuilder.AppendLine(item);
				}

				outputCallback.Invoke(logBuilder.ToString());

				logBuilder.Clear();
			}
		}
	}
}