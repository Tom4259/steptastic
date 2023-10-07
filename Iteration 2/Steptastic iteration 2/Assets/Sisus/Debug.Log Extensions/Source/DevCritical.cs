using System;
using System.Diagnostics;
using System.Linq.Expressions;
using UnityEngine;
using JetBrains.Annotations;
#if DEBUG_LOG_EXTENSIONS_INSIDE_UNIQUE_NAMESPACE
using static Sisus.Debugging.Debug;
#else
using static Debug;
#endif
using Object = UnityEngine.Object;

namespace Sisus.Debugging
{
	/// <summary>
	/// Utility class containing Debugging methods similar to the <see cref="Debug"/> class, with four main differences:
	/// 1. The messages use a larger font in the console window.
	/// 2. They always include stack trace even if they have been disabled for normal messages in Player Settings.
	/// 3. All calls to its methods — including any calls made in their arguments — are completely omitted
	/// in release builds.
	/// </summary>
	public static class DevCritical
	{
		private const string CriticalPrefix = "Critical!!" + "\r\n";

		public static bool UseLargeFont = true;
		public static StackTraceLogType StackTrace = StackTraceLogType.ScriptOnly;
		public static bool AlwaysIncludeInBuilds = true;
		public static bool IncludeCriticalPrefixInBuilds = true;

		[Conditional("DEBUG")]
		public static void Log([CanBeNull]object message, Object context = null)
		{
			#if DEBUG
			if(message == null)
			{
				if(IsLogTypeAllowed(LogType.Log))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, StackTrace);
					}

					string nullString = Null;
					Format(ref nullString);
					UnityEngine.Debug.Log(nullString, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Null, StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			string text = message as string;
			if(text != null)
			{
				if(ShouldHideMessage(text))
				{
					BroadcastLogMessageSuppressed(text, formatter.ColorizePlainText(text), StackTraceUtility.ExtractStackTrace(), LogType.Log, context);
					return;
				}

				if(IsLogTypeAllowed(LogType.Log))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, StackTrace);
					}

					text = formatter.ColorizePlainText(text);
					Format(ref text);
					UnityEngine.Debug.Log(text, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(text, StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			var classMember = message as Expression<Func<object>>;
			if(classMember != null)
			{
				if(IsLogTypeAllowed(LogType.Log))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, StackTrace);
					}

					text = formatter.ToStringColorized(classMember);
					Format(ref text);
					UnityEngine.Debug.Log(text, context != null ? context : ExpressionUtility.GetContext(classMember));

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(formatter.ToStringColorized(classMember), StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			if(IsLogTypeAllowed(LogType.Log))
			{
				text = formatter.ToStringColorized(message, false);
				Format(ref text);
				UnityEngine.Debug.Log(text, context);
			}
			else
			{
				LogCriticalToFile(formatter.ToStringUncolorized(message, false), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void Log([CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(ShouldHideMessage(message))
			{
				BroadcastLogMessageSuppressed(message, formatter.ColorizePlainText(message), StackTraceUtility.ExtractStackTrace(), LogType.Log, context);
				return;
			}

			if(IsLogTypeAllowed(LogType.Log))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, StackTrace);
				}

				message = formatter.ColorizePlainText(message);
				Format(ref message);
				UnityEngine.Debug.Log(message, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(message, StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void Log([NotNull]Expression<Func<object>> classMember)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Log))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, StackTrace);
				}

				string message = formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.Log(message, ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void Log([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Log))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, StackTrace);
				}

				string message = formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.Log(message, context != null ? context : ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(ShouldHideMessage(message))
			{
				BroadcastLogMessageSuppressed(message, formatter.ColorizePlainText(message), StackTraceUtility.ExtractStackTrace(), LogType.Warning, context);
				return;
			}

			if(IsLogTypeAllowed(LogType.Warning))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, StackTrace);
				}

				message = formatter.ColorizePlainText(message);
				Format(ref message);
				UnityEngine.Debug.LogWarning(message, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(message, StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([NotNull]Expression<Func<object>> classMember)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Warning))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, StackTrace);
				}

				string message = formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogWarning(message, ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Warning))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, StackTrace);
				}

				string message = formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogWarning(message, context != null ? context : ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogError([CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(ShouldHideMessage(message))
			{
				BroadcastLogMessageSuppressed(message, formatter.ColorizePlainText(message), StackTraceUtility.ExtractStackTrace(), LogType.Error, context);
				return;
			}

			if(IsLogTypeAllowed(LogType.Error))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				message = formatter.ColorizePlainText(message);
				Format(ref message);
				UnityEngine.Debug.LogError(message, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(message, StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogError([NotNull]Expression<Func<object>> classMember)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Error))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				string message = formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogError(message, ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogError([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Error))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				string message = formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogError(message, context != null ? context : ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, Object context = null)
		{
			#if DEBUG
			if(condition)
			{
				return;
			}

			if(IsLogTypeAllowed(LogType.Assert))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, StackTrace);
				}

				string message = "Assertation failed.";
				Format(ref message);
				UnityEngine.Debug.Assert(false, message, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile("Assertation failed", StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, object message, Object context = null)
		{
			#if DEBUG
			if(condition)
			{
				return;
			}

			if(IsLogTypeAllowed(LogType.Assert))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, StackTrace);
				}

				string text = formatter.ToStringColorized(message, false);
				Format(ref text);
				UnityEngine.Debug.Assert(false, text, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(formatter.ToStringUncolorized(message, false), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogException(Exception exception)
		{
			#if DEBUG
			if(IsLogTypeAllowed(LogType.Exception))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				string text = exception.ToString();
				Format(ref text);
				UnityEngine.Debug.LogError(text);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(exception.ToString(), StackTraceUtility.ExtractStackTrace());
			}
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogFormat<LogOption>(LogType logType, LogOption logOptions, Object context, string format, params object[] args) where LogOption : struct, IConvertible
		{
			#if DEBUG
			if(!IsLogTypeAllowed(logType))
			{
				return;
			}

			string text = string.Format(format, args);
			string textFormatted = formatter.FormatColorized(format, args);
			bool stackTrace = logOptions.Equals(default(LogOption));

			if(ShouldHideMessage(text))
			{
				BroadcastLogMessageSuppressed(text, textFormatted, stackTrace ? StackTraceUtility.ExtractStackTrace() : "", logType, context);
				return;
			}

			LastMessageUnformatted = text;
			LastMessageContext = context;

			#if UNITY_2019_1_OR_NEWER
			UnityEngine.Debug.LogFormat(logType, stackTrace ? UnityEngine.LogOption.None : UnityEngine.LogOption.NoStacktrace, context, textFormatted);
			#else
			var stackTraceLogTypeWas = Application.GetStackTraceLogType(logType);
			if(stackTrace)
			{
				if(stackTraceLogTypeWas == StackTraceLogType.None)
				{
					Application.SetStackTraceLogType(logType, StackTraceLogType.ScriptOnly);
				}
			}
			else if(stackTraceLogTypeWas != StackTraceLogType.None)
			{
				Application.SetStackTraceLogType(logType, StackTraceLogType.None);
			}

			switch(logType)
            {
                case LogType.Error:
				case LogType.Exception:
					UnityEngine.Debug.LogError(textFormatted, context);
					break;
                case LogType.Assert:
					UnityEngine.Debug.Assert(false, textFormatted, context);
					break;
                case LogType.Warning:
					UnityEngine.Debug.LogWarning(textFormatted, context);
					break;
                case LogType.Log:
					UnityEngine.Debug.Log(textFormatted, context);
                    break;
            }
			Application.SetStackTraceLogType(logType, stackTraceLogTypeWas);
			#endif
			#endif
		}

		#if DEBUG
		/// <summary>
		/// Logs critical message to Player.log file with CRITICAL!!! prefix and with stack trace included.
		/// </summary>
		private static void LogCriticalToFile(string message, string stackTrace)
		{
			LogToFile(CriticalPrefix + message + formatter.CleanUpStackTrace(stackTrace), "Player.log");
		}
		#endif

		#if DEBUG
		private static void Format(ref string message)
		{
			if(Application.isEditor && UseLargeFont)
			{
				message = formatter.FormatLarge(message);
			}
			else if(IncludeCriticalPrefixInBuilds)
			{
				message = CriticalPrefix + message;
			}
		}
		#endif
	}
}