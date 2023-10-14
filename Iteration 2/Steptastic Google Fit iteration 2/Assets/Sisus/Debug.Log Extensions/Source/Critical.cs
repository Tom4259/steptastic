using System;
using System.Linq.Expressions;
using UnityEngine;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus.Debugging
{
	/// <summary>
	/// Utility class containing Debugging methods similar to the <see cref="Debug"/> class, with three main differences:
	/// 1. The messages use a larger font in the console window.
	/// 2. They always include stack trace even if they have been disabled for normal messages in Player Settings.
	/// 3. The messages are always recorded in builds, even if "Use Player Log" is disabled in Player Settings.
	/// </summary>
	public static class Critical
	{
		private const string CriticalPrefix = "Critical!!" + "\r\n";

		public static bool UseLargeFont = true;
		public static StackTraceLogType StackTrace = StackTraceLogType.ScriptOnly;
		public static bool AlwaysIncludeInBuilds = true;
		public static bool IncludeCriticalPrefixInBuilds = true;

		/// <summary>
		/// Logs a <paramref name="message"/> to the console.
		/// <para>
		/// The message uses a larger font in the console window and always includes stack trace information.
		/// The message will also always be recorded in builds, even if "Use Player Log" is disabled in Player Settings.
		/// </para>
		/// </summary>
		/// <param name="message"> <see cref="object"/> to be converted to <see cref="string"/> representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		public static void Log([CanBeNull]object message, Object context = null)
		{
			if(message == null)
			{
				if(Debug.IsLogTypeAllowed(LogType.Log))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, StackTrace);
					}

					string nullString = Debug.Null;
					Format(ref nullString);
					UnityEngine.Debug.Log(nullString, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Debug.Null, StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			string text = message as string;
			if(text != null)
			{
				if(Debug.ShouldHideMessage(text))
				{
					Debug.BroadcastLogMessageSuppressed(text, Debug.formatter.ColorizePlainText(text, true), StackTraceUtility.ExtractStackTrace(), LogType.Log, context);
					return;
				}

				if(Debug.IsLogTypeAllowed(LogType.Log))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, StackTrace);
					}

					text = Debug.formatter.ColorizePlainText(text, true);
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
				if(Debug.IsLogTypeAllowed(LogType.Log))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, StackTrace);
					}

					text = Debug.formatter.ToStringColorized(classMember);
					Format(ref text);
					UnityEngine.Debug.Log(text, context != null ? context : ExpressionUtility.GetContext(classMember));

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Debug.formatter.ToStringColorized(classMember), StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Log))
			{
				text = Debug.formatter.ToStringColorized(message, false);
				Format(ref text);
				UnityEngine.Debug.Log(text, context);
			}
			else
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(message, false), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void Log([CanBeNull]string message, Object context = null)
		{
			if(Debug.ShouldHideMessage(message))
			{
				Debug.BroadcastLogMessageSuppressed(message, Debug.formatter.ColorizePlainText(message, true), StackTraceUtility.ExtractStackTrace(), LogType.Log, context);
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Log))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, StackTrace);
				}

				message = Debug.formatter.ColorizePlainText(message, true);
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
		}

		public static void Log([NotNull]Expression<Func<object>> classMember)
		{
			if(Debug.IsLogTypeAllowed(LogType.Log))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, StackTrace);
				}

				string message = Debug.formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.Log(message, ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void Log([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context)
		{
			if(Debug.IsLogTypeAllowed(LogType.Log))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Log);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, StackTrace);
				}

				string message = Debug.formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.Log(message, context != null ? context : ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Log, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
		}

		/// <summary>
		/// Logs a warning <paramref name="message"/> to the console.
		/// <para>
		/// The message uses a larger font in the console window and always includes stack trace information.
		/// The message will also always be recorded in builds, even if "Use Player Log" is disabled in Player Settings.
		/// </para>
		/// </summary>
		/// <param name="message"> <see cref="object"/> to be converted to <see cref="string"/> representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		public static void LogWarning([CanBeNull]object message, Object context = null)
		{
			if(message == null)
			{
				if(Debug.IsLogTypeAllowed(LogType.Warning))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Warning, StackTrace);
					}

					string nullString = Debug.Null;
					Format(ref nullString);
					UnityEngine.Debug.Log(nullString, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Debug.Null, StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			string text = message as string;
			if(text != null)
			{
				if(Debug.ShouldHideMessage(text))
				{
					Debug.BroadcastLogMessageSuppressed(text, Debug.formatter.ColorizePlainText(text, true), StackTraceUtility.ExtractStackTrace(), LogType.Warning, context);
					return;
				}

				if(Debug.IsLogTypeAllowed(LogType.Warning))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Warning, StackTrace);
					}

					text = Debug.formatter.ColorizePlainText(text, true);
					Format(ref text);
					UnityEngine.Debug.Log(text, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
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
				if(Debug.IsLogTypeAllowed(LogType.Warning))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Warning, StackTrace);
					}

					text = Debug.formatter.ToStringColorized(classMember);
					Format(ref text);
					UnityEngine.Debug.Log(text, context != null ? context : ExpressionUtility.GetContext(classMember));

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Debug.formatter.ToStringColorized(classMember), StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Warning))
			{
				text = Debug.formatter.ToStringColorized(message, false);
				Format(ref text);
				UnityEngine.Debug.Log(text, context);
			}
			else
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(message, false), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void LogWarning([CanBeNull]string message, Object context = null)
		{
			if(Debug.ShouldHideMessage(message))
			{
				Debug.BroadcastLogMessageSuppressed(message, Debug.formatter.ColorizePlainText(message, true), StackTraceUtility.ExtractStackTrace(), LogType.Warning, context);
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Warning))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, StackTrace);
				}

				message = Debug.formatter.ColorizePlainText(message, true);
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
		}

		public static void LogWarning([NotNull]Expression<Func<object>> classMember)
		{
			if(Debug.IsLogTypeAllowed(LogType.Warning))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, StackTrace);
				}

				string message = Debug.formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogWarning(message, ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void LogWarning([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context)
		{
			if(Debug.IsLogTypeAllowed(LogType.Warning))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Warning);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, StackTrace);
				}

				string message = Debug.formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogWarning(message, context != null ? context : ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Warning, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
		}

		/// <summary>
		/// Logs an error <paramref name="message"/> to the console.
		/// <para>
		/// The message uses a larger font in the console window and always includes stack trace information.
		/// The message will also always be recorded in builds, even if "Use Player Log" is disabled in Player Settings.
		/// </para>
		/// </summary>
		/// <param name="message"> <see cref="object"/> to be converted to <see cref="string"/> representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		public static void LogError([CanBeNull]object message, Object context = null)
		{
			if(message == null)
			{
				if(Debug.IsLogTypeAllowed(LogType.Error))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Error, StackTrace);
					}

					string nullString = Debug.Null;
					Format(ref nullString);
					UnityEngine.Debug.Log(nullString, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Debug.Null, StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			string text = message as string;
			if(text != null)
			{
				if(Debug.ShouldHideMessage(text))
				{
					Debug.BroadcastLogMessageSuppressed(text, Debug.formatter.ColorizePlainText(text, true), StackTraceUtility.ExtractStackTrace(), LogType.Error, context);
					return;
				}

				if(Debug.IsLogTypeAllowed(LogType.Error))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Error, StackTrace);
					}

					text = Debug.formatter.ColorizePlainText(text, true);
					Format(ref text);
					UnityEngine.Debug.Log(text, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
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
				if(Debug.IsLogTypeAllowed(LogType.Error))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Error, StackTrace);
					}

					text = Debug.formatter.ToStringColorized(classMember);
					Format(ref text);
					UnityEngine.Debug.Log(text, context != null ? context : ExpressionUtility.GetContext(classMember));

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
					}
				}
				else if(AlwaysIncludeInBuilds)
				{
					LogCriticalToFile(Debug.formatter.ToStringColorized(classMember), StackTraceUtility.ExtractStackTrace());
				}
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Error))
			{
				text = Debug.formatter.ToStringColorized(message, false);
				Format(ref text);
				UnityEngine.Debug.Log(text, context);
			}
			else
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(message, false), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void LogError([CanBeNull]string message, Object context = null)
		{
			if(Debug.ShouldHideMessage(message))
			{
				Debug.BroadcastLogMessageSuppressed(message, Debug.formatter.ColorizePlainText(message, true), StackTraceUtility.ExtractStackTrace(), LogType.Error, context);
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Error))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				message = Debug.formatter.ColorizePlainText(message, true);
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
		}

		public static void LogError([NotNull]Expression<Func<object>> classMember)
		{
			if(Debug.IsLogTypeAllowed(LogType.Error))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				string message = Debug.formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogError(message, ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void LogError([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context)
		{
			if(Debug.IsLogTypeAllowed(LogType.Error))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Error);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, StackTrace);
				}

				string message = Debug.formatter.ToStringColorized(classMember);
				Format(ref message);
				UnityEngine.Debug.LogError(message, context != null ? context : ExpressionUtility.GetContext(classMember));

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Error, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(classMember), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void Assert(bool condition, Object context = null)
		{
			if(condition)
			{
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Assert))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, StackTrace);
				}

				string message = Debug.AssertionFailedMessage;
				Format(ref message);
				UnityEngine.Debug.Assert(false, message, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.AssertionFailedMessage, StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void Assert(bool condition, object message, Object context = null)
		{
			if(condition)
			{
				return;
			}

			if(Debug.IsLogTypeAllowed(LogType.Assert))
			{
				var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, StackTrace);
				}

				string text = Debug.formatter.ToStringColorized(message, false);
				Format(ref text);
				UnityEngine.Debug.Assert(false, text, context);

				if(stackTraceTypeWas != StackTrace)
				{
					Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				LogCriticalToFile(Debug.formatter.ToStringUncolorized(message, false), StackTraceUtility.ExtractStackTrace());
			}
		}

		public static void LogException(Exception exception)
		{
			if(Debug.IsLogTypeAllowed(LogType.Exception))
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
		}

		/// <summary>
		/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>
		/// and returns <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/> or <see langword="false"/> if it was not.
		/// <example>
		/// <code>
		/// float Divide(float dividend, float divisor)
		/// {
		///		return Critical.Ensure(divisor != 0f) ? dividend / divisor : 0f;
		/// }
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
		/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
        /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
		public static bool Ensure(bool condition, Object context = null)
		{
			if(condition)
			{
				return true;
			}

			if(Debug.IsLogTypeAllowed(LogType.Assert))
			{
				if(Debug.failedEnsures.Add(StackTraceUtility.ExtractStackTrace()))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, StackTrace);
					}

					string message = Debug.AssertionFailedMessage;
					Format(ref message);
					UnityEngine.Debug.Assert(false, message, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
					}
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				string stackTrace = StackTraceUtility.ExtractStackTrace();
				if(Debug.failedEnsures.Add(stackTrace))
				{
					LogCriticalToFile(Debug.AssertionFailedMessage, stackTrace);
				}
			}

			return false;
		}

		/// <summary>
		/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>
		/// and returns <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/> or <see langword="false"/> if it was not.
		/// <example>
		/// <code>
		/// float Divide(float dividend, float divisor)
		/// {
		///		return Critical.Ensure(divisor != 0f) ? dividend / divisor : 0f;
		/// }
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs if logged. </param>
		/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>		
		/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
        /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
		public static bool Ensure(int channel, bool condition, Object context = null)
		{
			if(condition)
			{
				return true;
			}
			
			if(Debug.IsLogTypeAllowed(LogType.Assert))
			{
				if(Debug.failedEnsures.Add(StackTraceUtility.ExtractStackTrace()))
				{
					if(!Debug.channels.IsEnabled(channel))
					{
						Debug.BroadcastLogMessageSuppressed("[" + Channels.Get(channel) + "] " + Debug.GuardFailedMessage,
							Debug.formatter.GetColorizedChannelPrefix(channel) + " " + Debug.GuardFailedMessage,
							StackTraceUtility.ExtractStackTrace(), LogType.Assert, context);
						return false;
					}

					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, StackTrace);
					}

					string message = Debug.formatter.GetColorizedChannelPrefix(channel) + " " + Debug.EnsureFailedMessage;
					Format(ref message);
					UnityEngine.Debug.Assert(false, message, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
					}
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				string stackTrace = StackTraceUtility.ExtractStackTrace();
				if(Debug.failedEnsures.Add(stackTrace))
				{
					LogCriticalToFile(Debug.EnsureFailedMessage, stackTrace);
				}
			}

			return false;
		}

		/// <summary>
		/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>
		/// and returns <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/> or <see langword="true"/> if it was not.
		/// <para>
		/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
		/// </para>
		/// <example>
		/// <code>
		/// void CopyComponent(Component component, GameObject to)
		/// {
		///		if(Critical.Guard(component != null && to != null))
		///		{
		///			return;
		///		}
		///		
		///		var copy = to.AddComponent(component.GetType());
		///		var json = JsonUtility.ToJson(component);
		///		JsonUtility.FromJsonOverwrite(json, copy);
		/// }
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
		/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
        /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
		public static bool Guard(bool condition, Object context = null)
		{
			if(condition)
			{
				return false;
			}
			
			if(Debug.IsLogTypeAllowed(LogType.Assert))
			{
				if(Debug.failedGuards.Add(StackTraceUtility.ExtractStackTrace()))
				{
					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, StackTrace);
					}

					string message = Debug.GuardFailedMessage;
					Format(ref message);
					UnityEngine.Debug.Assert(false, message, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
					}
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				string stackTrace = StackTraceUtility.ExtractStackTrace();
				if(Debug.failedGuards.Add(stackTrace))
				{
					LogCriticalToFile(Debug.GuardFailedMessage, stackTrace);
				}
			}

			return true;
		}

		/// <summary>
		/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>
		/// and returns <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/> or <see langword="true"/> if it was not.
		/// <para>
		/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
		/// </para>
		/// <example>
		/// <code>
		/// void CopyComponent(Component component, GameObject to)
		/// {
		///		if(Critical.Guard(component != null && to != null))
		///		{
		///			return;
		///		}
		///		
		///		var copy = to.AddComponent(component.GetType());
		///		var json = JsonUtility.ToJson(component);
		///		JsonUtility.FromJsonOverwrite(json, copy);
		/// }
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs if logged. </param>
		/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>		
		/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
        /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
		public static bool Guard(int channel, bool condition, Object context = null)
		{
			if(condition)
			{
				return false;
			}
			
			if(Debug.IsLogTypeAllowed(LogType.Assert))
			{
				if(Debug.failedGuards.Add(StackTraceUtility.ExtractStackTrace()))
				{
					if(!Debug.channels.IsEnabled(channel))
					{
						Debug.BroadcastLogMessageSuppressed("[" + Channels.Get(channel) + "] " + Debug.GuardFailedMessage,
							Debug.formatter.GetColorizedChannelPrefix(channel) + " " + Debug.GuardFailedMessage,
							StackTraceUtility.ExtractStackTrace(), LogType.Assert, context);
						return true;
					}

					var stackTraceTypeWas = Application.GetStackTraceLogType(LogType.Assert);
					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, StackTrace);
					}

					string message = Debug.formatter.GetColorizedChannelPrefix(channel) + " " + Debug.GuardFailedMessage;
					Format(ref message);
					UnityEngine.Debug.Assert(false, message, context);

					if(stackTraceTypeWas != StackTrace)
					{
						Application.SetStackTraceLogType(LogType.Assert, stackTraceTypeWas);
					}
				}
			}
			else if(AlwaysIncludeInBuilds)
			{
				string stackTrace = StackTraceUtility.ExtractStackTrace();
				if(Debug.failedGuards.Add(stackTrace))
				{
					LogCriticalToFile(Debug.GuardFailedMessage, stackTrace);
				}
			}

			return true;
		}

		/// <summary>
		/// Throws an <see cref="Exception">exception</see> of type <typeparamref name="TException"/> if <paramref name="condition"/> is not <see langword="true"/>.
		/// <para>
		/// This can be useful for checking that the arguments passed to a function are valid and if not aborting.
		/// </para>
		/// <para>
		/// Note that an exception will be thrown every time that this method is called and <paramref name="condition"/> evaluates to <see langword="false"/>.
		/// This is in contrast to some of the other Guard methods that only log an error once per session.
		/// </para>
		/// <example>
		/// <code>
		/// void CopyComponent(Component component, GameObject to)
		/// {
		///		Critical.Guard<ArgumentNullException>(component != null, nameof(component)));
		///		Critical.Guard<ArgumentNullException>(to != null, nameof(to));
		///		
		///		var copy = to.AddComponent(component.GetType());
		///		var json = JsonUtility.ToJson(component);
		///		JsonUtility.FromJsonOverwrite(json, copy);
		/// }
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
		/// <param name="exceptionArguments">
		/// Optional arguments to pass to the <typeparamref name="TException"/> constructor when an exception is thrown.
		/// <para>
		/// Most exception constructors accept a <see cref="string"/> argument specifying the message to be logged to the Console.
		/// For <see cref="ArgumentNullException"/> on the other hand a single <see cref="string"/> argument specifies the name of the
		/// parameter that was <see langword="null"/>.
		/// </para>
		/// </param>
		public static void Guard<TException>(bool condition, params object[] exceptionArguments) where TException : Exception, new()
		{
			if(condition)
			{
				return;
			}
			if(exceptionArguments == null || exceptionArguments.Length == 0)
			{
				throw new TException();
			}
			throw Activator.CreateInstance(typeof(TException), exceptionArguments) as TException;
		}

		public static void LogFormat<LogOption>(LogType logType, LogOption logOptions, Object context, string format, params object[] args) where LogOption : struct, IConvertible
		{
			if(!Debug.IsLogTypeAllowed(logType))
			{
				return;
			}

			string text = string.Format(format, args);
			string textFormatted = Debug.formatter.FormatColorized(format, args);
			bool stackTrace = logOptions.Equals(default(LogOption));

			if(Debug.ShouldHideMessage(text))
			{
				Debug.BroadcastLogMessageSuppressed(text, textFormatted, stackTrace ? StackTraceUtility.ExtractStackTrace() : "", logType, context);
				return;
			}

			Debug.LastMessageUnformatted = text;
			Debug.LastMessageContext = context;

			#if !UNITY_2018_4
			UnityEngine.Debug.LogFormat(logType, stackTrace ? UnityEngine.LogOption.None : UnityEngine.LogOption.NoStacktrace, context, textFormatted);
			#endif
		}

		/// <summary>
		/// Logs critical message to Player.log file with CRITICAL!!! prefix and with stack trace included.
		/// </summary>
		private static void LogCriticalToFile(string message, string stackTrace)
		{
			Debug.LogToFile(CriticalPrefix + message + Debug.formatter.CleanUpStackTrace(stackTrace), "Player.log");
		}

		private static void Format(ref string message)
		{
			#if UNITY_EDITOR
			if(UseLargeFont)
			{
				message = Debug.formatter.FormatLarge(message);
				return;
			}
			#else
			if(IncludeCriticalPrefixInBuilds)
			{
				message = CriticalPrefix + message;
			}
			#endif
		}
	}
}