
using System;
using System.Runtime.Serialization;

namespace Modules.Net
{
	[Serializable]
	public sealed class NetworkReachabilityException : Exception
	{
		public NetworkReachabilityException() : base() { }

		public NetworkReachabilityException(string message) : base(message) { }

		public NetworkReachabilityException(string message, Exception innerException) : base(message, innerException) { }

		private NetworkReachabilityException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}