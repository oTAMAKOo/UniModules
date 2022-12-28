
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace Modules.Net.WebRequest
{
	public sealed class UnityWebRequestErrorException : Exception
	{
		public string RawErrorMessage { get; private set; }
		public bool HasResponse { get; private set; }
		public string Text { get; private set; }
		public System.Net.HttpStatusCode StatusCode { get; private set; }
		public Dictionary<string, string> ResponseHeaders { get; private set; }
		public UnityWebRequest Request { get; private set; }

		// cache the text because if www was disposed, can't access it.
		public UnityWebRequestErrorException(UnityWebRequest request)
		{
			Request = request;
			RawErrorMessage = request.error;
			ResponseHeaders = request.GetResponseHeaders();
			HasResponse = false;
			Text = GetResponseText(request);
			StatusCode = (System.Net.HttpStatusCode)request.responseCode;

			if (request.responseCode != 0)
			{
				HasResponse = true;
			}
		}

		private static string GetResponseText(UnityWebRequest request)
		{
			if (request.downloadHandler == null){ return null; }

			if (request.downloadHandler is DownloadHandlerFile){ return null; }

			return request.downloadHandler.text;
		}

		public override string ToString()
		{
			var text = Text;

			if (string.IsNullOrEmpty(text))
			{
				return RawErrorMessage;
			}
			else
			{
				return RawErrorMessage + " " + text;
			}
		}
	}
}
