using System;

namespace Sisus.Debugging.Settings
{
	[Serializable]
	public sealed class IgnoredStackTraceInfo
	{
		public string namespaceName = "";
		public string className = "";
		public string methodName = "";
	}
}