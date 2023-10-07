namespace Sisus.Debugging
{
	public sealed class IgnoredStackTraceInfo
	{
		public readonly string namespaceName = "";
		public readonly string className = "";
		public readonly string methodName = "";

		public IgnoredStackTraceInfo(string namespaceName, string className, string methodName)
		{
			this.namespaceName = namespaceName;
			this.className = className;
			this.methodName = methodName;
		}
	}
}