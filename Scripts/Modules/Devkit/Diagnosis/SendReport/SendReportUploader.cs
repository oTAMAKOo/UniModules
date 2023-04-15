
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Devkit.Diagnosis.SendReport
{
	public sealed class SendReportUploader : ISendReportUploader
	{
		//----- params -----

		public enum DataFormat
		{
			Form,
			Json,
		}

		public sealed class Result
        {
            public long ResponseCode { get; private set; }
			
            public string Text { get; private set; }
			
            public byte[] Bytes { get; private set; }
			
            public string Error { get; private set; }

			public bool HasError { get; private set; }

            public Result(UnityWebRequest request)
            {
                ResponseCode = request.responseCode;
                Text = request.downloadHandler.text;
                Bytes = request.downloadHandler.data;
                Error = request.error;
            }
        }

		//----- field -----

		private string reportUrl = null;

		private DataFormat format = DataFormat.Form;

		//----- property -----

		//----- method -----

		public SendReportUploader(string reportUrl, DataFormat format)
		{
			this.reportUrl = reportUrl;
			this.format = format;
		}

		public async UniTask<SendReportResult> Upload(string reportTitle, Dictionary<string, string> reportContents, IProgress<float> progress, CancellationToken cancelToken)
		{
			SendReportResult result = null;

			if (string.IsNullOrEmpty(reportUrl))
			{
				throw new Exception("report url is empty.");
			}

			UnityWebRequest webRequest = null;

			switch (format)
			{
				case DataFormat.Form:
					webRequest = UnityWebRequest.Post(reportUrl, CreateReportFormSections(reportContents));
					break;

				case DataFormat.Json:
					webRequest = UnityWebRequest.Post(reportUrl, CreateReportJson(reportContents));
					break;
			}

			webRequest.timeout = 30;

			try
			{
				await webRequest.SendWebRequest().ToUniTask(progress, cancellationToken: cancelToken);

				result = new SendReportResult(webRequest);
			}
			catch (OperationCanceledException)
			{
				/* Canceled */

				webRequest.Abort();
			}

			return result;
		}

        private List<IMultipartFormSection> CreateReportFormSections(Dictionary<string, string> reportContents)
		{
			var reportForm = new List<IMultipartFormSection>();

			foreach (var item in reportContents)
			{
				reportForm.Add(new MultipartFormDataSection(item.Key, item.Value));
			}

			return reportForm;
		}

		private string CreateReportJson(Dictionary<string, string> reportContents)
		{
			return reportContents.ToJson();
		}
	}
}