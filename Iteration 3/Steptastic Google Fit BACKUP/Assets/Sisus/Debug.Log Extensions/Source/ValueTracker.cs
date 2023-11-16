using System;
using System.Linq.Expressions;
using System.Text;
using Object = UnityEngine.Object;

namespace Sisus.Debugging
{
	public class ValueTracker : IValueTracker
	{
		private static readonly StringBuilder sb = new StringBuilder();

		private readonly Expression<Func<object>> trackedMember;
		private readonly Func<object> valueGetter;
		private readonly Object context;
		private readonly bool pauseOnChanged;
		private readonly DebugFormatter formatter;

		private object lastValue;
		private bool formatLastValueAsPlainText;

		public ValueTracker(Expression<Func<object>> trackMember, bool setPauseOnChanged, DebugFormatter setFormatter)
		{
			trackedMember = trackMember;

			var lambdaExpression = (LambdaExpression)trackMember;
			valueGetter = (Func<object>)lambdaExpression.Compile();

			context = ExpressionUtility.GetOwner(trackedMember);
			pauseOnChanged = setPauseOnChanged;
			formatter = setFormatter;

			lastValue = valueGetter();

			// If target method is string.Format then skip string formatting.
			// This makes it look better when using string interpolation.
			formatLastValueAsPlainText = false;
			var methodCallExpression = trackMember.Body as MethodCallExpression;
			if(methodCallExpression == null)
			{
				return;
			}
			var method = methodCallExpression.Method;
			if(method == null || method.DeclaringType != typeof(string) || !string.Equals(method.Name, "Format"))
			{
				return;
			}
			formatLastValueAsPlainText = true;
		}

		public bool TargetEquals(object other)
		{
			var otherExpression = other as Expression<Func<object>>;
			return otherExpression != null && ExpressionUtility.Equals(trackedMember, otherExpression);
		}

		/// <summary>
		/// This should be called during every Update, LateUpdate or a comparable event function.
		/// <para>
		/// Logs a message to the console if the value tracked member has changed since last update.
		/// </para>
		/// </summary>
		public void Update()
		{
			var newValue = valueGetter();
			if(ReferenceEquals(newValue, null))
			{
				if(ReferenceEquals(lastValue, null))
				{
					return;
				}
			}
			else if(newValue.Equals(lastValue))
			{
				return;
			}

			var unityObject = newValue as Object;
			if(unityObject == null)
			{
				unityObject = context;
			}

			formatter.ToStringColorized(trackedMember, sb, true);
			sb.Append("\n(was: ");
			sb.Append(formatLastValueAsPlainText ? formatter.ColorizePlainText(lastValue as string) : formatter.ToStringColorized(lastValue, true));
			sb.Append(")");
			string messageFormatted = formatter.Format(sb.ToString());
			sb.Length = 0;

			formatter.ToStringUncolorized(trackedMember, sb, true);
			sb.Append("\n(was: ");
			sb.Append(formatLastValueAsPlainText ? lastValue : formatter.ToStringUncolorized(lastValue, true));
			sb.Append(")");
			string messageUnformatted = formatter.Format(sb.ToString());
			sb.Length = 0;

			Debug.LastMessageUnformatted = messageUnformatted;
			Debug.LastMessageContext = unityObject;
			UnityEngine.Debug.Log(messageFormatted, unityObject);
			sb.Length = 0;

			lastValue = newValue;

			if(pauseOnChanged)
			{
				UnityEngine.Debug.Break();
			}
		}

		public override bool Equals(object obj)
		{
			var tracker = obj as ValueTracker;
			return tracker != null && ExpressionUtility.Equals(trackedMember, tracker.trackedMember);
		}

		public override int GetHashCode() => trackedMember.ToString().GetHashCode();
	}
}