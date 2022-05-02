
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Net.WebRequest;

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

		public IObservable<string> Upload(string reportTitle, Dictionary<string, string> reportContents, IProgress<float> progress)
		{
			return Observable.FromMicroCoroutine<string>(observer => PostReport(observer, reportContents, progress));
		}

		private IEnumerator PostReport(IObserver<string> observer, Dictionary<string, string> reportContents,  IProgress<float> progress)
		{
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

			var operation = webRequest.SendWebRequest();

			while (!operation.isDone)
			{
				if (progress != null)
				{
					progress.Report(operation.progress);
				}

				yield return null;
			}

			var errorMessage = string.Empty;

			if (webRequest.HasError())
			{
				errorMessage = string.Format("[{0}]{1}", webRequest.responseCode, webRequest.error);
			}

			observer.OnNext(errorMessage);
			observer.OnCompleted();
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