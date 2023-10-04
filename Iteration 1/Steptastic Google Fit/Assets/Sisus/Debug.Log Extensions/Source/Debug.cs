using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

#if !DEBUG_LOG_EXTENSIONS_INSIDE_UNIQUE_NAMESPACE
using Sisus.Debugging;
#endif

#if DEBUG_LOG_EXTENSIONS_INSIDE_UNIQUE_NAMESPACE
namespace Sisus.Debugging
{
#endif

public delegate void LoggingMessage(string textUnformatted, string textFormatted, Object context, LogType logType);

public delegate void OnMessageSuppressed(string messageUnformatted, string messageFormatted, string stackTrace, LogType type, Object context);

/// <summary>
/// Extended version of the built-in Debug class with additional methods to ease debugging while developing a game.
/// </summary>
public static class Debug
{
	internal const string AssertionFailedMessage = "Assertion failed.";
	internal const string EnsureFailedMessage = "Ensure failed.";
	internal const string GuardFailedMessage = "Guard failed.";

	private const MethodImplOptions AggressiveInlining = (MethodImplOptions)256; // MethodImplOptions.AggressiveInlining only exists in .NET 4.5. and later
	private const string NameStateSeparator = " state: ";

	public static readonly List<IUpdatable> DebuggedEveryFrame = new List<IUpdatable>();
	public static readonly List<IDisplayable> DisplayedOnScreen = new List<IDisplayable>();
	public static event OnMessageSuppressed LogMessageSuppressed;
	[NotNull]
	public static DebugFormatter formatter = new DebugFormatter();
	[CanBeNull]
	public static Object LastMessageContext;

	[CanBeNull]
	public static string LastMessageUnformatted;

	[NotNull]
	public static readonly Channels channels = new Channels();

	internal static readonly HashSet<string> failedEnsures = new HashSet<string>();
	internal static readonly HashSet<string> failedGuards = new HashSet<string>();
	internal const BindingFlags DefaultInstanceBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
	internal const BindingFlags DefaultStaticBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

	private static readonly HashSet<string> loggedToFiles = new HashSet<string>();
	private static bool usePlayerLog = true;

	#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
	private static readonly List<NestableStopwatch> stopwatches = new List<NestableStopwatch>(4);
	private static int nextStopwatchNameIndex = 0;
	private static string[] stopwatchNames = new string[] { "Stopwatch 1", "Stopwatch 2", "Stopwatch 3", "Stopwatch 4", "Stopwatch 5" };
	private static readonly FpsCounter FpsCounter = new FpsCounter();
	#endif

	/// <summary>
	/// Reports whether messages logged using the <see cref="Debug"/> class are being recorded to a log file.
	/// <para>
	/// If this is <see langword="false"/> then only messages logged using the <see cref="Critical"/> class or
	/// messages written to log files manually using LogToFile will be recorded.
	/// </para>
	/// </summary>
	public static bool UsePlayerLog
	{
		get
		{
			return usePlayerLog;
		}
	}

	/// <summary>
	/// Reports whether the development Console is visible.
	/// </summary>
	public static bool developerConsoleVisible
	{
		get
		{
			return UnityEngine.Debug.developerConsoleVisible;
		}
	}

	/// <summary>
	/// In the Build Settings dialog there is a check box called "Development Build".
	/// <para>
	/// If it is checked <see cref="isDebugBuild"/> will be <see langword="true"/>. In the editor isDebugBuild always returns <see langword="true"/>.
	/// </para>
	/// <para>
	/// When "Strip Log Calls From Builds" is checked in Project Settings for Console, then logging done using the <see cref="Debug"/> class will be stripped from non-development builds.
	/// </para>
	/// <para>
	/// Additionally logging done using the <see cref="Dev"/> class will always be stripped from non-development builds, regardless of whether or not "Strip Log Calls From Builds" is checked.
	/// </para>
	/// <para>
	/// Logging done using the <see cref="Critical"/> class will always be recorded even in non-development builds, regardless of whether or not "Strip Log Calls From Builds" is checked.
	/// </para>
	/// </summary>
	public static bool isDebugBuild
	{
		get
		{
			return UnityEngine.Debug.isDebugBuild;
		}
	}

	/// <summary>
	/// Gets the default debug logger used internally for logging all messages.
	/// </summary>
	public static ILogger unityLogger
	{
		get
		{
			return UnityEngine.Debug.unityLogger;
		}
	}

	internal static string Null
	{
		get
		{
			return formatter.Null;
		}
	}

	public static void Initialize()
	{
		DebugLogExtensionsProjectSettings.Get();

		if(Application.isMobilePlatform && !Application.isEditor)
		{
			usePlayerLog = UnityEngine.Debug.unityLogger.logEnabled;
		}
		else
		{
			string path = Application.consoleLogPath;
			usePlayerLog = !string.IsNullOrEmpty(path) && File.Exists(path);
		}
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([CanBeNull]object message)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([CanBeNull]object message, [CanBeNull]Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console on the given <paramref name="channel"/>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log(Channel.Audio, "Playing {audioId} in {delay} seconds.", this);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel, [CanBeNull]object message, [CanBeNull]Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, channel, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console on the given channels.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySoundEffect(float delay, AudioId audioId)
	/// {
	/// 	Debug.Log(Channel.Audio, Channel.Sfx, "Playing {audioId} in {delay} seconds.", this);
	/// 	
	/// 	yield return new WaitForSeconds(delay);
	/// 	
	/// 	audioController.Play(audioId);
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
	/// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel1, int channel2, [CanBeNull]object message, [CanBeNull]Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, channel1, channel2, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([CanBeNull]string message)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([CanBeNull]string message, [CanBeNull]Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console on the given <paramref name="channel"/>.
	/// <para>
	/// Channels can be used to selectively suppress messages you don't care about at the moment.
	/// </para>
    /// <example>
	/// <code>
	/// void LogAudioEvent(string message)
	/// {
	/// 	Debug.Log(Channel.Audio, message, this);
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console on the given channels.
	/// <para>
	/// Channels can be used to selectively suppress messages you don't care about at the moment.
	/// </para>
    /// <example>
	/// <code>
	/// void LogSfxEvent(string message)
	/// {
	/// 	Debug.Log(Channel.Audio, Channel.Sfx, message, this);
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> First channel to which the message belongs. </param>
	/// <param name="channel2"> Second channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel1, int channel2, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, LogType.Log, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console along with method signature on the second line to provide additional context.
	/// <para>
	/// If the method belongs to a <see cref="UnityEngine.Object"/> context information will be automatically added to the message.
	/// </para>
    /// <example>
	/// <code>
	/// public void DestroyIfNotNull(Object target)
	/// {
	///		if(target == null)
	///		{
	///			return;
	///		}
	///
	/// 	Debug.Log("Destroying target.", ()=>Destroy(target));
	///
    /// 	Object.Destroy(target);
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="methodContext"> Expression pointing to a method to which the message relates. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([CanBeNull]string message, [NotNull]Expression<Action> methodContext)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, methodContext, LogType.Log, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of <paramref name="classMember">class member</paramref> along with method signature on the second line to provide additional context.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		
	///		Debug.Log(()=>activePage, ()=>SetActivePage(value));
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="methodContext"> Expression pointing to a method to which the message relates. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([NotNull]Expression<Func<object>> classMember, [NotNull]Expression<Action> methodContext)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(classMember, methodContext, LogType.Log, 0, 0, null, null);
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of <paramref name="classMember">a class member</paramref>.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		
	///		Debug.Log(()=>activePage, this);
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(classMember, LogType.Log, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs to the Console on the given <paramref name="channel"/> the name and value of <paramref name="classMember">a class member</paramref>.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		
	///		Debug.Log(()=>activePage, this);
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel, [NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(classMember, LogType.Log, channel, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console consisting of a <paramref name="prefix">text string</paramref> followed by the names and values of
	/// <paramref name="classMembers">zero or more class members</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log("[Audio] Playing delayed - ", ()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(soundId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prefix"> Prefix text for the message. </param>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([NotNull]string prefix, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		bool hide = ShouldHideMessage(prefix);
		LogInternal(prefix, classMembers, LogType.Log, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console formed by joining the given text strings together.
	/// </summary>
	/// <param name="messagePart"> First part of the message. </param>
	/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(string messagePart, params string[] messageParts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
		LogInternal(formatter.JoinUncolorized(messagePart, messageParts), formatter.JoinColorized(messageParts), LogType.Log, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console formed by joining the given text strings together.
	/// </summary>
	/// <param name="messagePart1"> First part of the message. </param>
	/// <param name="messagePart2"> Second part of the message. </param>
	/// <param name="messageParts"> Additional parts of the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(string messagePart1, string messagePart2, params string[] messageParts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(formatter.JoinUncolorized(messagePart1, messagePart2, messageParts), formatter.JoinColorized(messagePart1, messagePart2, messageParts), LogType.Log, 0, 0, null, ShouldHideMessage(messagePart1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console listing a number of elements separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
	/// </summary>
	/// <param name="arg1"> First listed element. </param>
	/// <param name="arg2"> Second listed element. </param>
	/// <param name="args"> (Optional) Additional listed elements. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(object arg1, object arg2, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		LogInternal(arg1, arg2, args, LogType.Log, 0, 0, null, ShouldHideMessage(arg1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console consisting of a <paramref name="prefix">text string</paramref> followed
	/// by the values of <paramref name="args">zero or more objects</paramref> separated by a separator character.
	/// <para>
	/// A comma will be used for the separator character with shorter messages and a line break with longer messages.
	/// </para>
	/// <example>
	/// <code>
	/// public void TestLog123()
	/// {
	///		Debug.Log("Test: ", 1, 2, 3);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prefix"> Prefix text for the message. </param>
	/// <param name="arg"> First listed element after the prefix. </param>
	/// <param name="args"> (Optional) Additional listed elements after the prefix. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(string prefix, object arg, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		LogInternal(prefix, arg, args, LogType.Log, 0, 0, null, ShouldHideMessage(prefix) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console consisting of multiple parts joined together.
	/// </summary>
	/// <param name="part1"> First part of the message. </param>
	/// <param name="part2"> Second part of the message. </param>
	/// <param name="parts"> (Optional) Additional parts of the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(Object context, string part1, string part2, params string[] parts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(formatter.JoinUncolorized(part1, part2, parts), formatter.JoinColorized(part1, part2, parts), LogType.Log, 0, 0, context, ShouldHideMessage(part1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of one or more <paramref name="classMembers">class members</paramref> separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log([NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);
		LogInternal(classMembers, LogType.Log, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs to the Console on the given <paramref name="channel"/> the name and value of zero or more <paramref name="classMembers">class members</paramref> separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> <see cref="Channel"/> to which the message belongs. </param>
	/// <param name="classMembers"> An expression pointing to a class members whose names and values will be included in the message. </param>
	/// <param name="classMembers"> Additional expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel, Expression<Func<object>> classMember, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);
		LogInternal(classMembers, LogType.Log, channel, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs the name and value of one or more class members to the Console separated by a separator character using the given channels.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> First channel to which the message belongs. </param>
	/// <param name="channel2"> Second channel to which the message belongs. </param>
	/// <param name="classMember"> An expressions pointing to a class member whose name and value will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel1, int channel2, Expression<Func<object>> classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		LogInternal(classMember, LogType.Log, channel1, channel2, null, null);
		#endif
	}

	/// <summary>
	/// Logs the name and value of one or more class members to the Console separated by a separator character using the given channels.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> First channel to which the message belongs. </param>
	/// <param name="channel2"> Second channel to which the message belongs. </param>
	/// <param name="classMember"> An expressions pointing to a class member whose name and value will be included in the message. </param>
	/// <param name="classMembers"> Additional expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Log(int channel1, int channel2, Expression<Func<object>> classMember, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		LogInternal(classMember, classMembers, LogType.Log, channel1, channel2, null, null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.Log("Playing {0} in {1} seconds.", audioId, delay);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat(string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Log, 0, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogFormat(this, "Playing {0} in {1} seconds.", audioId, delay);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat(Object context, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Log, 0, 0, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console on the given <paramref name="channel"/>, formed by inserting the values of <paramref name="args">zero or more objects</paramref>
	/// into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogFormat(Channel.Audio, "Playing {0} in {1} seconds.", audioId, delay);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/>
	/// string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat(int channel, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Log, channel, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console on the given channels, formed by inserting the values of <paramref name="args">zero or more objects</paramref>
	/// into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySoundEffect(float delay, AudioId audioId)
	/// {
	///		Debug.Log(Channel.Audio, Channel.Sfx, "Playing {0} in {1} seconds.", audioId, delay);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
	/// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/>
	/// string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat(int channel1, int channel2, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Log, channel1, channel2, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console on the given channels, formed by inserting the values of <paramref name="args">zero or more objects</paramref>
	/// into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySoundEffect(float delay, AudioId audioId)
	/// {
	///		Debug.Log(Channel.Audio, Channel.Sfx, "Playing {0} in {1} seconds.", audioId, delay);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
	/// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/>
	/// string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat(int channel1, int channel2, Object context, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Log, channel1, channel2, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
	/// </summary>
	/// <param name="logType"> Type of the message; Log, Warning, Error, Assert or Exception. </param>
	/// <param name="logOptions"> Option flags for specifying special treatment of a log message. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat<LogOption>(LogType logType, LogOption logOptions, Object context, string format, params object[] args) where LogOption : struct, IConvertible
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(logType))
		{
			return;
		}

		var stackTraceLogTypeWas = Application.GetStackTraceLogType(logType);
		if(logOptions.Equals(default(LogOption)))
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

		LogFormatInternal(format, args, logType, 0, 0, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);

		Application.SetStackTraceLogType(logType, stackTraceLogTypeWas);

		#endif
	}

	/// <summary>
	/// Logs a message to the Console on the given channels, formed by inserting the values of <paramref name="args">zero or more objects</paramref>
	/// into a <paramref name="format">text string</paramref>.
	/// </summary>
	/// <param name="channel1"> First channel to which the message belongs. </param>
	/// <param name="channel2"> Second channel to which the message belongs. </param>	
	/// <param name="logType"> Type of the message; Log, Warning, Error, Assert or Exception. </param>
	/// <param name="logOptions"> Option flags for specifying special treatment of a log message. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/>
	/// string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogFormat<LogOption>(int channel1, int channel2, LogType logType, LogOption logOptions, Object context, string format, params object[] args) where LogOption : struct, IConvertible
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(logType))
		{
			return;
		}

		var stackTraceLogTypeWas = Application.GetStackTraceLogType(logType);
		if(logOptions.Equals(default(LogOption)))
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

		LogFormatInternal(format, args, logType, channel1, channel2, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);

		Application.SetStackTraceLogType(logType, stackTraceLogTypeWas);
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of every field and property of <paramref name="target"/> matched using the specified <paramref name="flags"/>.
	/// <para>
	/// With a small number of listed members a comma will be used to separate them, and with a larger number of members a line break will be used.
	/// </para>
	/// </summary>
	/// <param name="target"> <see cref="object"/> instance whose class members are to be listed. </param>
	/// <param name="flags">
	/// <see cref="BindingFlags"/> used when searching for the members.
	/// <para>
	/// By default only public and non-inherited instance members are listed.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogState([CanBeNull]object target, BindingFlags flags = DefaultInstanceBindingFlags)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		if(target == null)
		{
			LogInternal(formatter.NullUncolorized, Null, LogType.Log, 0, 0, target as Object, null);
			return;
		}
		LogStateInternal(target.GetType(), target, flags, 0, 0, null);
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of every field and property of <paramref name="target"/> matched using the specified settings.
	/// <para>
	/// With a small number of listed members a comma will be used to separate them, and with a larger number of members a line break will be used.
	/// </para>
	/// </summary>
	/// <param name="target"> <see cref="object"/> instance whose members are to be listed. </param>
	/// <param name="includePrivate"> If <see langword="false"/> then only public members will be listed; otherwise, non-public members will also be listed. </param>
	/// <param name="includeStatic"> If <see langword="false"/> then only instance members will be listed; otherwise, static members will also be listed. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogState([CanBeNull]object target, bool includePrivate, bool includeStatic = false)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		if(target == null)
		{
			LogInternal(formatter.NullUncolorized, Null, LogType.Log, 0, 0, target as Object, null);
			return;
		}

		BindingFlags flags;
		if(includePrivate)
		{
			if(includeStatic)
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
			}
			else
			{
				flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			}
		}
		else if(includeStatic)
		{
			flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
		}
		else
		{
			flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
		}
		var classType = target.GetType();
		LogStateInternal(classType, target, flags, 0, 0, null);
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of every static field and property of <paramref name="classType"/> matched using the specified <paramref name="flags"/>.
	/// <para>
	/// With a small number of listed members a comma will be used to separate them, and with a larger number of members a line break will be used.
	/// </para>
	/// </summary>
	/// <param name="classType"> <see cref="Type"/> of the class whose members are to be listed. </param>
	/// <param name="flags">
	/// <see cref="BindingFlags"/> used when searching for the members.
	/// <para>
	/// By default only public and non-inherited static members are listed.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogState([NotNull]Type classType, BindingFlags flags = DefaultStaticBindingFlags)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		LogStateInternal(classType, null, flags, 0, 0, null);
		#endif
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="true"/> logs a <paramref name="message"/> to the Console.
	/// <para>
	/// If <paramref name="condition"/> is <see langword="false"/> does nothing.
	/// </para>
	/// </summary>
	/// <param name="condition"> Condition that must be <see langword="true"/> for logging to take place. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogIf(bool condition, string message, Object context = null)
    {
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!condition || !IsLogTypeAllowed(LogType.Log))
        {
			return;
        }
		LogInternal(message, LogType.Log, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
    }

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="true"/> logs to the Console on the given <paramref name="channel"/>
	/// a <paramref name="message"/>.
	/// <para>
	/// If <paramref name="condition"/> is <see langword="false"/> does nothing.
	/// </para>
	/// </summary>
	/// <param name="condition"> Condition that must be <see langword="true"/> for logging to take place. </param>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogIf(int channel, bool condition, string message, Object context = null)
    {
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!condition || !IsLogTypeAllowed(LogType.Log))
        {
			return;
        }
		LogInternal(message, LogType.Log, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
    }

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="true"/> logs to the Console on the given <paramref name="channel"/>
	/// the name and value of <paramref name="classMember">a class member</paramref>.
	/// <para>
	/// If <paramref name="condition"/> is <see langword="false"/> does nothing.
	/// </para>
	/// </summary>
	/// <param name="condition"> Condition that must be <see langword="true"/> for logging to take place. </param>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogIf(int channel, bool condition, [NotNull] Expression<Func<object>> classMember, Object context = null)
    {
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!condition || !IsLogTypeAllowed(LogType.Log))
        {
			return;
        }
		LogInternal(classMember, LogType.Log, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
    }

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console using a large font size.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogLarge([CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, formatter.FormatLarge(formatter.ColorizePlainText(message)), LogType.Log, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null, true);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console using a large font size and the given channel.
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogLarge(int channel, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, formatter.FormatLarge(formatter.ColorizePlainText(message)), LogType.Log, channel, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null, true);
		#endif
	}

	/// <summary>
	/// Logs a <paramref name="message"/> to the Console using a large font size and the given channels.
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
    /// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogLarge(int channel1, int channel2, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}
		LogInternal(message, formatter.FormatLarge(formatter.ColorizePlainText(message)), LogType.Log, channel1, channel2, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null, true);
		#endif
	}

	/// <summary>
	/// Appends a message to the end of the log file.
	/// <para>
	/// Note that calls to this method will function even in release builds with build stripping enabled.
	/// As such it is possible to use this method to log some critical messages manually even if otherwise
	/// all logging has been disabled across the project.
	/// </para>
	/// </summary>
	/// <param name="message"> Message to add to the log file. </param>
	/// <param name="logFilePath"> Path to the log file to which the message should be added. </param>
	/// <param name="clearFile">
	/// <list type="number">
	/// <item>
	/// <description>
	/// <see cref="ClearFile.OnSessionStart"/> : The log file is cleared before the <paramref name="message"/> is written to it
	/// if this is the first time during this session that <see cref="LogToFile"/> is called for this <paramref name="logFilePath">file path</paramref>.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <see cref="ClearFile.Now"/> : The log file is cleared now before the message is written to it.
	/// <para>
	/// This might be an useful if you have a log file dedicated to holding only one type of information like system information or performance metrics,
	/// and you want to write all the information at once to the log file, replacing its old contents entirely with each call.
	/// </para>
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <see cref="ClearFile.Never"/> : This method call will never clear the log file.
	/// <para> Note that with this option the log file will continue growing larger with each call of this method until it is manually cleared by calling <see cref="ClearLogFile"/>.
	/// </para>
	/// </description>
	/// </item>
	/// </list>
	/// </param>
	public static void LogToFile(string message, [CanBeNull] string logFilePath = null, ClearFile clearFile = ClearFile.OnSessionStart)
	{
		var fullPath = GetFullLogFilePath(logFilePath);

		if(loggedToFiles.Add(fullPath))
		{
			if(clearFile != ClearFile.Never)
			{
				ClearLogFile(fullPath, true);
				clearFile = ClearFile.Now;
			}
		}

		var directoryPath = Path.GetDirectoryName(fullPath);
		if(!Directory.Exists(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		if(clearFile == ClearFile.Now)
		{
			File.WriteAllText(fullPath, message);
		}
		else
		{
			File.AppendAllText(fullPath, Environment.NewLine + message);
		}
	}

	/// <summary>
	/// Deletes existing log file at the given path created by a <see cref="LogToFile"/> call if one exists.
	/// </summary>
	/// <param name="path"> Path to the log file. </param>
	/// <param name="backupExisting"> If <see langword="true"/> a backup is created of the log file before it is deleted, if one is found. </param>
	public static void ClearLogFile(string path = null, bool backupExisting = true)
	{
		var fullPath = GetFullLogFilePath(path);
		if(!File.Exists(fullPath))
		{
			return;
		}

		if(backupExisting)
		{
			string backupPath = Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath) + "-prev.log");
			File.Copy(fullPath, backupPath, true);
		}
		File.Delete(fullPath);
	}

	/// <summary>
	/// Logs to the Console the name and value of <paramref name="classMember"/> any time its value is changed.
	/// <para>
	/// This will continue happening until <see cref="CancelLogChanges(MemberInfo)"/> is called with an
	/// expression pointing to the same class member.
	/// </para>
	/// <para>
	/// At runtime logging takes place at the end of each frame.
	/// </para>
	/// </summary>
	/// <param name="classMember"> Expression pointing to the class member to track. </param>
	/// <param name="pauseOnChanged"> If <see langword="true"/>
	/// then the editor will be paused whenever the value of the class member changes; otherwise, editor will not be paused.
	/// <para>
	/// In builds this parameter will have no effect; the application will not be paused regardless of its value.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogChanges(Expression<Func<object>> classMember, bool pauseOnChanged = false)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		foreach(var debugged in DebuggedEveryFrame)
		{
			if(debugged.TargetEquals(classMember) && debugged is ValueTracker)
			{
				return;
			}
		}

		DebuggedEveryFrame.Add(new ValueTracker(classMember, pauseOnChanged, formatter));
		#endif
	}

	/// <summary>
	/// Logs to the Console the name and value of <paramref name="classMember"/> any time its value is changed.
	/// <para>
	/// This will continue happening until <see cref="CancelLogChanges(MemberInfo)"/> is called with an
	/// expression pointing to the same class member.
	/// </para>
	/// <para>
	/// At runtime logging takes place at the end of each frame.
	/// </para>
	/// </summary>
	/// <param name="memberOwner">
	/// Instance of the class that contains the member and from which the value of the member is read.
	/// <para>
	/// This can be <see langword="null"/> if the <see cref="MemberInfo"/> represents a static member.
	/// </para>
	/// </param>
	/// <param name="classMember">
	/// <see cref="MemberInfo"/> representing the class member to track.
	/// </param>
	/// <param name="pauseOnChanged"> If <see langword="true"/>
	/// then the editor will be paused whenever the value of the class member changes; otherwise, editor will not be paused.
	/// <para>
	/// In builds this parameter will have no effect; the application will not be paused regardless of its value.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogChanges([CanBeNull]object memberOwner, [NotNull]MemberInfo classMember, bool pauseOnChanged = false)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		foreach(var debugged in DebuggedEveryFrame)
		{
			if(debugged.TargetEquals(classMember) && debugged is MemberInfoValueTracker)
			{
				return;
			}
		}

		DebuggedEveryFrame.Add(new MemberInfoValueTracker(memberOwner, classMember, pauseOnChanged, formatter));
		#endif
	}

	/// <summary>
	/// Stop logging to the Console any time the value of <paramref name="classMember"/> changes.
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member that is being tracked. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelLogChanges(Expression<Func<object>> classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		for(int i = DebuggedEveryFrame.Count - 1; i >= 0; i--)
		{
			if(DebuggedEveryFrame[i].TargetEquals(classMember) && DebuggedEveryFrame[i] is ValueTracker)
			{
				DebuggedEveryFrame.RemoveAt(i);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Stop logging to the Console any time the value of <paramref name="classMember"/> changes.
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member that is being tracked. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelLogChanges([NotNull]MemberInfo classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		for(int i = DebuggedEveryFrame.Count - 1; i >= 0; i--)
		{
			if(DebuggedEveryFrame[i].TargetEquals(classMember) && DebuggedEveryFrame[i] is MemberInfoValueTracker)
			{
				DebuggedEveryFrame.RemoveAt(i);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Clears all value trackers that have been enabled using <see cref="LogChanges"/>.
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void ClearTrackedValues()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		for(int i = DebuggedEveryFrame.Count - 1; i >= 0; i--)
		{
			if(DebuggedEveryFrame[i] is IValueTracker)
			{
				DebuggedEveryFrame.RemoveAt(i);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([CanBeNull]object message)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([CanBeNull]object message, [CanBeNull]Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([CanBeNull]string message)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([CanBeNull]string message, [CanBeNull]Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console on the given <paramref name="channel"/>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogWarning(Channel.Audio, "Playing {audioId} in {delay} seconds.", this);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(int channel, [CanBeNull]object message, [CanBeNull]Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console on the given <paramref name="channel"/>.
	/// <para>
	/// Channels can be used to selectively suppress messages you don't care about at the moment.
	/// </para>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(int channel, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console on the given channels.
	/// <para>
	/// Channels can be used to selectively suppress messages you don't care about at the moment.
	/// </para>
	/// </summary>
	/// <param name="channel1"> First channel to which the message belongs. </param>
	/// <param name="channel2"> Second channel to which the message belongs. </param>	
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(int channel1, int channel2, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(message, LogType.Warning, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console listing a number of elements separated by a separator character.
	/// 
	/// With shorter messages a comma will be used for the separator character, and with longer message a line break will be used.
	/// </summary>
	/// <param name="arg1"> First listed element. </param>
	/// <param name="arg2"> Second listed element. </param>
	/// <param name="args"> (Optional) Additional listed elements. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(object arg1, object arg2, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		LogInternal(arg1, arg2, args, LogType.Warning, 0, 0, null, ShouldHideMessage(arg1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console consisting of a <paramref name="prefix">text string</paramref> followed by the names and values of
	/// <paramref name="classMembers">zero or more class members</paramref>.
	/// <para>
	/// A comma will be used for the separator character with shorter messages and a line break with longer messages.
	/// </para>
	/// <example>
	/// <code>
	/// public void TestLogWarning123()
	/// {
	///		Debug.LogWarning("Test: ", 1, 2, 3);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prefix"> Prefix text for the message. </param>
	/// <param name="arg"> First listed element after the prefix. </param>
	/// <param name="args"> Additional listed elements after the prefix. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(string prefix, object arg, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		LogInternal(prefix, arg, args, LogType.Warning, 0, 0, null, ShouldHideMessage(prefix) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console consisting of multiple parts joined together.
	/// </summary>
	/// <param name="part1"> First part of the message. </param>
	/// <param name="part2"> Second part of the message. </param>
	/// <param name="parts"> (Optional) Additional parts of the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(Object context, string part1, string part2, params string[] parts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(formatter.JoinUncolorized(part1, part2, parts), formatter.JoinColorized(part1, part2, parts), LogType.Warning, 0, 0, context, ShouldHideMessage(part1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console consisting of the name and value of one or more class members separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
	/// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogWarning(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(classMembers, LogType.Warning, 0, 0, null, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message consisting of the name and value of a class member to the Console.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		Debug.LogWarning(()=>activePage, this);
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([NotNull]Expression<Func<object>> classMember, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(classMember, LogType.Warning, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console on the given <paramref name="channel"/> and consisting of the name and value of a class member.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		Debug.LogWarning(()=>activePage, this);
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(int channel, [NotNull]Expression<Func<object>> classMember, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(classMember, LogType.Warning, channel, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message consisting of the name and value of a class member to the Console.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		Debug.LogWarning(()=>activePage, this);
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
	/// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(int channel1, int channel2, [NotNull]Expression<Func<object>> classMember, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(classMember, LogType.Warning, channel1, channel2, context, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console consisting of a <paramref name="prefix">text string</paramref> followed by the names and values of
	/// <paramref name="classMembers">zero or more class members</paramref>.
	/// <para>
	/// A comma will be used for the separator character with shorter messages and a line break with longer messages.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogWarning("[Audio] PlaySound called with an invalid delay - ", ()=>delay, ()=>audioId);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prefix"> Prefix text for the message. </param>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning([NotNull]string prefix, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(prefix, classMembers, LogType.Warning, 0, 0, null, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by joining the given text strings together.
	/// </summary>
	/// <param name="messagePart1"> First part of the message. </param>
	/// <param name="messagePart2"> Second part of the message. </param>
	/// <param name="messageParts"> Additional parts of the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(string messagePart1, string messagePart2, params string[] messageParts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogInternal(formatter.JoinUncolorized(messagePart1, messagePart2, messageParts), formatter.JoinColorized(messagePart1, messagePart2, messageParts), LogType.Warning, 0, 0, null, ShouldHideMessage(messagePart1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by joining the given text strings together.
	/// </summary>
	/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarning(params string[] messageParts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
		LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Warning, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
	/// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogWarningFormat("PlaySound({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarningFormat(string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Warning, 0, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogWarningFormat(this, "PlaySound({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarningFormat(Object context, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Warning, 0, 0, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogWarningFormat(Channel.Audio, this, "PlaySound({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarningFormat(int channel, Object context, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Warning, channel, 0, context, channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySoundEffect(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogWarningFormat(Channel.Audio, Channel.Sfx, this, "PlaySoundEffect({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
	/// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarningFormat(int channel1, int channel2, Object context, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Warning, channel1, channel2, context, channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySoundEffect(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogWarningFormat(Channel.Audio, Channel.Sfx, "PlaySoundEffect({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs. </param>
	/// <param name="channel2"> The second channel to which the message belongs. </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarningFormat(int channel1, int channel2, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Warning, channel1, channel2, null, channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console using a large font size.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogWarningLarge([CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Warning))
		{
			return;
		}

		LogInternal(message, formatter.FormatLarge(formatter.ColorizePlainText(message)), LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null, true);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([CanBeNull]string message)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([CanBeNull]string message, [CanBeNull]Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([CanBeNull]object message)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, 0, 0, null, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console.
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([CanBeNull]object message, [CanBeNull]Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs a warning <paramref name="message"/> to the Console on the given <paramref name="channel"/>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogWarning(Channel.Audio, "Playing {audioId} in {delay} seconds.", this);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(int channel, [CanBeNull]object message, [CanBeNull]Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console on the given <paramref name="channel"/>.
	/// <para>
	/// Channels can be used to selectively suppress messages you don't care about at the moment.
	/// </para>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(int channel, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console on the given channels.
	/// <para>
	/// Channels can be used to selectively suppress messages you don't care about at the moment.
	/// </para>
	/// </summary>
	/// <param name="channel1"> First channel to which the message belongs. </param>
	/// <param name="channel2"> Second channel to which the message belongs. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(int channel1, int channel2, [CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, LogType.Error, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error to the Console listing a number of elements separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used for the separator character, and with longer message a line break will be used.
	/// </para>
	/// </summary>
	/// <param name="arg1"> First listed element. </param>
	/// <param name="arg2"> Second listed element. </param>
	/// <param name="args"> (Optional) Additional listed elements. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(object arg1, object arg2, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(arg1, arg2, args, LogType.Error, 0, 0, null, ShouldHideMessage(arg1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error to the Console starting with <paramref name="prefix"/> and followed
	/// by a list of elements separated by a separator character.
	/// <para>
	/// Comma will be used for the separator character with short messages and line break with longer messages.
	/// </para>
	/// <example>
	/// <code>
	/// public void TestLogError123()
	/// {
	///		Debug.LogError("Test: ", 1, 2, 3);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prefix"> Prefix text for the message. </param>
	/// <param name="arg"> First listed element after the prefix. </param>
	/// <param name="args"> (Optional) Additional listed elements after the prefix. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(string prefix, object arg, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(prefix, arg, args, LogType.Error, 0, 0, null, ShouldHideMessage(prefix) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console consisting of multiple parts joined together.
	/// </summary>
	/// <param name="part1"> First part of the message. </param>
	/// <param name="part2"> Second part of the message. </param>
	/// <param name="parts"> (Optional) Additional parts of the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(Object context, string part1, string part2, params string[] parts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(formatter.JoinUncolorized(part1, part2, parts), formatter.JoinColorized(part1, part2, parts), LogType.Error, 0, 0, context, ShouldHideMessage(part1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console consisting of the name and value of one or more class members separated by a separator character.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogError(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
	/// </summary>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);
		LogInternal(classMembers, LogType.Error, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console on the given <paramref name="channel"/> and consisting of the name and value of one or more class members separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogError(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel"> <see cref="Channel"/> to which the message belongs. </param>
	/// <param name="classMember"> An expressions pointing to class members whose name and value will be included in the message. </param>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(int channel, Expression<Func<object>> classMember, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(classMember, classMembers, LogType.Error, channel, 0, null, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console on the given channels and consisting of the name and value of
	/// <paramref name="classMembers">zero or more class members</paramref> separated by a separator character.
	/// <para>
	/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		Debug.LogError(()=>delay, ()=>audioId);
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first <see cref="Channel"/> to which the message belongs. </param>
	/// <param name="channel2"> The second <see cref="Channel"/> to which the message belongs. </param>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(int channel1, int channel2, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);
		LogInternal(classMembers, LogType.Error, channel1, channel2, context, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console consisting of the name and value of <paramref name="classMember">a class member</paramref>.
    /// <example>
    /// <code>
    /// public void SetActivePage(Page value)
    /// {
	///		activePage = value;
	///		Debug.LogError(()=>activePage, this);
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([NotNull]Expression<Func<object>> classMember, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		if(context == null)
		{
			context = ExpressionUtility.GetContext(classMember);
		}
		LogInternal(classMember, LogType.Error, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console consisting of a <paramref name="prefix">text string</paramref> followed by the names and values of
	/// <paramref name="classMembers">zero or more class members</paramref>.
	/// <para>
	/// A comma will be used for the separator character with shorter messages and a line break with longer messages.
	/// </para>
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogError("[Audio] PlaySound called with an invalid delay - ", ()=>delay, ()=>audioId);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prefix"> Prefix text for the message. </param>
	/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError([NotNull]string prefix, [NotNull]params Expression<Func<object>>[] classMembers)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);		
		LogInternal(prefix, classMembers, LogType.Error, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error to the Console formed by joining the given text strings together.
	/// </summary>
	/// <param name="messagePart1"> First part of the message. </param>
	/// <param name="messagePart2"> Second part of the message. </param>
	/// <param name="messageParts"> Additional parts of the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(string messagePart1, string messagePart2, params string[] messageParts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(formatter.JoinUncolorized(messagePart1, messagePart2, messageParts), formatter.JoinColorized(messagePart1, messagePart2, messageParts), LogType.Error, 0, 0, null, ShouldHideMessage(messagePart1) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error to the Console formed by joining the given text strings together.
	/// </summary>
	/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogError(params string[] messageParts)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
		LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Error, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
	/// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogErrorFormat("PlaySound({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogErrorFormat(string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Error, 0, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogErrorFormat(this, "PlaySound({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogErrorFormat(Object context, string format, params object[] args)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Error, 0, 0, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an error <paramref name="message"/> to the Console using a large font size.
	/// </summary>
	/// <param name="message"> Message to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogErrorLarge([CanBeNull]string message, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Error))
		{
			return;
		}
		LogInternal(message, formatter.FormatLarge(formatter.ColorizePlainText(message)), LogType.Error, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null, true);
		#endif
	}

	/// <summary>
	/// Logs an <paramref name="exception"/> to the Console.
	/// </summary>
	/// <param name="exception"> Runtime exception to display. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogException(Exception exception)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Exception))
		{
			return;
		}

		/*
		#if DEV_MODE //TEST - this can be used to log with cleaner stack trace. however it breaks double-click to go to line feature.
		var was = UnityEditor.PlayerSettings.GetStackTraceLogType(LogType.Exception);
		if(was == StackTraceLogType.ScriptOnly)
		{
			UnityEditor.PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.None);
			UnityEngine.Debug.unityLogger.Log(LogType.Exception, formatter.FormatWithStackTrace(exception.ToString(), StackTraceUtility.ExtractStackTrace()));
			UnityEditor.PlayerSettings.SetStackTraceLogType(LogType.Exception, was);
		}
		#endif
		*/

		LogInternal(exception, 0, 0, null, null);
		#endif
	}

	/// <summary>
	/// Logs an <paramref name="exception"/> to the Console.
	/// </summary>
	/// <param name="exception"> Runtime exception to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogException(Exception exception, Object context)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Exception))
		{
			return;
		}
		LogInternal(exception, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an <paramref name="exception"/> to the Console on the given <paramref name="channel"/>.
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs. </param>
	/// <param name="exception"> Runtime exception to display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogException(int channel, Exception exception, Object context = null)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Exception))
		{
			return;
		}
		LogInternal(exception, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an <see cref="LogType.Assert">assertion</see> <paramref name="message"/> to the Console.
	/// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
	/// </summary>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context">
	/// <see cref="Object"/> to which the message applies.
	/// <para>
	/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogAssertion(object message, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(!IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogInternal(message, LogType.Assert, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// Logs an <see cref="LogType.Assert">assertion</see> message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
    /// <example>
	/// <code>
	/// public IEnumerator PlaySound(float delay, AudioId audioId)
	/// {
	///		if(delay < 0f)
	///		{
	///			Debug.LogErrorFormat("PlaySound({0}) called with an invalid delay: {1}.", audioId, delay);
	///			delay = 0f;
	///		}
	///		
	///		yield return new WaitForSeconds(delay);
	///		
	///		audioController.Play(audioId);
	///	}
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="format">
	/// A composite format string based on which the message is generated.
	/// <para>
	/// Each format item inside the string is replaced by the value of the argument at the same index.
	/// </para>
	/// <para>
	/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that location.
	/// </para>
	/// </param>
	/// <param name="args">
	/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
	/// </para>
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogAssertionFormat(string format, params object[] args)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(!IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Assert, 0, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void LogAssertionFormat(Object context, string format, params object[] args)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(!IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogFormatInternal(format, args, LogType.Assert, 0, 0, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
    /// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
    /// </summary>
	/// <param name="channel"> The channel to which the message belongs if logged. </param>
    /// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
    /// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(int channel, bool condition, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogInternal(AssertionFailedMessage, AssertionFailedMessage, LogType.Assert, channel, 0, context, null);
		#endif
	}

	/// <summary>
    /// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
    /// </summary>
	/// <param name="channel1"> The first channel to which the message belongs if logged. </param>
	/// <param name="channel2"> The second channel to which the message belongs if logged. </param>
    /// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>	
    /// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(int channel1, int channel2, bool condition, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogInternal(AssertionFailedMessage, AssertionFailedMessage, LogType.Assert, channel1, channel2, context, null);
		#endif
	}

	/// <summary>
    /// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
    /// </summary>
    /// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
    /// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(bool condition, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogInternal(AssertionFailedMessage, AssertionFailedMessage, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
	/// </summary>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(bool condition, object message, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}

		LogInternal(message, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs if logged. </param>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>	
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(int channel, bool condition, object message, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}

		LogInternal(message, LogType.Assert, channel, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs if logged. </param>
	/// <param name="channel2"> The second channel to which the message belongs if logged. </param>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>	
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(int channel1, int channel2, bool condition, object message, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}

		LogInternal(message, LogType.Assert, channel1, channel2, context, null);
		#endif
	}

	/// <summary>
    /// Logs an error message to the Console if expression does not return <see langword="true"/>.
    /// <example>
	/// <code>
	/// public void Divide(int dividend, int divisor)
	/// {
	/// 	Debug.Assert(()=>divisor != 0, ()=> Divide(dividend, divisor));
    /// 	
    ///		return (float) dividend / divisor;
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="condition"> <see cref="bool">Boolean</see> expression you expect to return <see langword="true"/>. For example a lambda expression. </param>
	/// <param name="contextMethod"> Expression pointing to a method to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(Expression<Func<bool>> condition, [NotNull]Expression<Action> contextMethod)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(!IsLogTypeAllowed(LogType.Assert) || IsTrue(condition))
		{
			return;
		}
		var context = ExpressionUtility.GetOwner(contextMethod);
		LogInternal(condition, contextMethod, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
    /// <example>
    /// <code>
    /// public void DestroyAll(params Object[] targets)
    /// {
    ///		Debug.Assert(targets != null, "DestroyAll called with null params.", ()=>Destroy(targets));
    ///		
    ///		foreach(var target in targets)
    ///		{
    ///			Debug.Assert(target != null, "DestroyAll called with a null target.", ()=>Destroy(targets));
    ///			
    ///			Destroy(target);
    ///		}
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="message"> Message to display. </param>
	/// <param name="methodContext"> Expression pointing to a method to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(bool condition, string message, [NotNull]Expression<Action> methodContext)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		var context = ExpressionUtility.GetOwner(methodContext);
		LogInternal(message, methodContext, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
    /// <example>
    /// <code>
    /// public void DestroyAll(params Object[] targets)
    /// {
    ///		Debug.Assert(targets != null, ()=>Destroy(targets));
    ///		
    ///		foreach(var target in targets)
    ///		{
    ///			Debug.Assert(target != null, ()=>Destroy(targets));
    ///			
    ///			Destroy(target);
    ///		}
    ///	}
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="methodContext"> Expression pointing to a method to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert(bool condition, [NotNull]Expression<Action> methodContext)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		var context = ExpressionUtility.GetOwner(methodContext);
		LogInternal(methodContext, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="classMember"/> value does not match <paramref name="expectedValue"/>.
    /// <para>
    /// Note that this method work only if UNITY_ASSERTIONS symbol is defined, like for example in development builds.
    /// </para>
    /// <example>
    /// <code>
    /// int variable = 5;
    /// variable += 10;
    /// Debug.Assert(15, ()=>variable);
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="expectedValue"> Value you expect the class member to have. </param>
	/// <param name="classMember"> Expression pointing to the class member with the expected value. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert([CanBeNull]object expectedValue, [NotNull]Expression<Func<object>> classMember)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(!IsLogTypeAllowed(LogType.Assert) || Equals(classMember, expectedValue))
		{
			return;
		}
		Object context = ExpressionUtility.GetOwner(classMember);
		LogInternal(classMember, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="classMember"/> value is not <see langword="true"/>.
	/// <para>
	/// Class member can be of type <see cref="bool"/> or any type that implements <see cref="IConvertible"/>.
	/// </para>
	/// <para>
	/// Class member can also be of type <see cref="Object"/> or any other class type <see cref="object"/>
	/// in which case an error will be logged if its value is <see langword="null"/>.
	/// </para>
    /// <para>
    /// Note that this method only works if UNITY_ASSERTIONS symbol is defined, like for example in development builds.
    /// </para>
    /// <example>
    /// <code>
    /// public int GetValue()
    /// {
    ///		bool valueFound = dictionary.TryGetValue(out int value);
    ///		Debug.Assert(()=>valueFound);
    ///		return value;
    /// }
    /// </code>
    /// </example>
	/// </summary>
	/// <param name="classMember"> Expression pointing to the class member with the expected value. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Assert([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(!IsLogTypeAllowed(LogType.Assert) || IsTrue(classMember))
		{
			return;
		}
		if(context == null)
        {
			context = ExpressionUtility.GetContext(classMember);
        }
		LogInternal(classMember, LogType.Assert, 0, 0, context, null);
		#endif
	}

	/// <summary>
    /// Logs a warning message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
    /// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
    /// </summary>
    /// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
    /// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void AssertWarning(bool condition, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogInternal(AssertionFailedMessage, AssertionFailedMessage, LogType.Warning, 0, 0, context, null);
		#endif
	}

	/// <summary>
	/// Logs a warning message to the Console if <paramref name="condition"/> is not <see langword="true"/>.
    /// <para>
	/// Calls to this method will be stripped from release builds unless the UNITY_ASSERTIONS symbol is defined.
    /// </para>
	/// </summary>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#else
	[Conditional("UNITY_ASSERTIONS")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void AssertWarning(bool condition, object message, Object context = null)
	{
		#if UNITY_ASSERTIONS && (!DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG)
		if(condition || !IsLogTypeAllowed(LogType.Assert))
		{
			return;
		}
		LogInternal(message, LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
		#endif
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error message and returns <see langword="false"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// </summary>
	/// <example>
	/// <code>
	/// private float Divide(float dividend, float divisor)
	/// {
	///		return Debug.Ensure(divisor != 0f) ? dividend / divisor : 0f;
	/// }
	/// </code>
	/// </example>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Ensure(bool condition, Object context = null)
	{
		if(condition)
		{
			return true;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedEnsures.Add(stackTrace))
			{
				LogInternal(EnsureFailedMessage, EnsureFailedMessage, LogType.Assert, 0, 0, context, null);
			}
		}
		#endif

		return false;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given <paramref name="channel"/>
	/// an error message and returns <see langword="false"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// </summary>
	/// <example>
	/// <code>
	/// private float Divide(float dividend, float divisor)
	/// {
	///		return Debug.Ensure(divisor != 0f) ? dividend / divisor : 0f;
	/// }
	/// </code>
	/// </example>
	/// <param name="channel"> The channel to which the message belongs if logged. </param>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Ensure(int channel, bool condition, Object context = null)
	{
		if(condition)
		{
			return true;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedEnsures.Add(stackTrace))
			{
				LogInternal(EnsureFailedMessage, EnsureFailedMessage, LogType.Assert, channel, 0, context, !channels.IsEnabled(channel) ? stackTrace : null);
			}
		}
		#endif

		return false;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given channels
	/// an error message and returns <see langword="false"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// <example>
	/// <code>
	/// private float Divide(float dividend, float divisor)
	/// {
	///		return Debug.Ensure(divisor != 0f) ? dividend / divisor : 0f;
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="channel1"> The first channel to which the message belongs if logged. </param>
	/// <param name="channel2"> The second channel to which the message belongs if logged. </param>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Ensure(int channel1, int channel2, bool condition, Object context = null)
	{
		if(condition)
		{
			return true;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedEnsures.Add(stackTrace))
			{
				LogInternal(EnsureFailedMessage, EnsureFailedMessage, LogType.Assert, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? stackTrace : null);
			}
		}
		#endif

		return false;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error <paramref name="message"/> and returns <see langword="false"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// <example>
	/// <code>
	/// private float Divide(float dividend, float divisor)
	/// {
	///		return Debug.Ensure(divisor != 0f) ? dividend / divisor : 0f;
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>
	/// <param name="message"> Message to display if <paramref name="condition"/> is <see langword="false"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Ensure(bool condition, string message, Object context = null)
	{
		if(condition)
		{
			return true;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
        {
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedEnsures.Add(stackTrace))
			{
				LogInternal(message, LogType.Assert, 0, 0, context, ShouldHideMessage(message) ? stackTrace : null);
			}
		}
		#endif

		return false;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given <paramref name="channel"/>
	/// an error <paramref name="message"/> and returns <see langword="false"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and only executing a block of code if so.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// <example>
	/// <code>
	/// private float Divide(float dividend, float divisor)
	/// {
	///		return Debug.Ensure(divisor != 0f) ? dividend / divisor : 0f;
	/// }
	/// </code>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
	/// </example>
	/// </summary>
	/// <param name="channel"> The channel to which the message belongs if logged. </param>
	/// <param name="condition"> Condition you expect to be <see langword="true"/>. </param>	
	/// <param name="message"> Message to display if <paramref name="condition"/> is <see langword="false"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Ensure(int channel, bool condition, string message, Object context = null)
	{
		if(condition)
		{
			return true;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedEnsures.Add(stackTrace))
			{
				LogInternal(message, formatter.ColorizePlainText(message), LogType.Assert, channel, 0, context, !channels.IsEnabled(channel) ? stackTrace : null);
			}
		}
		#endif

		return false;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error message and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null && to != null))
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
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(bool condition, Object context = null)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(GuardFailedMessage, GuardFailedMessage, LogType.Assert, 0, 0, context, null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given <paramref name="channel"/>
	/// an error message and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>
	/// and returns <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/> or <see langword="false"/> if it was not.
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null && to != null))
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
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(int channel, bool condition, Object context = null)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(GuardFailedMessage, GuardFailedMessage, LogType.Assert, channel, 0, context, !channels.IsEnabled(channel) ? stackTrace : null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given channels an error <paramref name="message"/>
	/// and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null && to != null))
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
	/// <param name="channel1"> The first channel to which the message belongs if logged. </param>
	/// <param name="channel2"> The second channel to which the message belongs if logged. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
    /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(int channel1, int channel2, bool condition, Object context = null)
	{
		if(condition)
		{
			return false;
		}
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(GuardFailedMessage, GuardFailedMessage, LogType.Assert, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? stackTrace : null);
			}
		}
		#endif
		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error <paramref name="message"/> and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null && to != null, "CopyComponent called with a null argument."))
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
	/// <param name="message"> Message to display if <paramref name="condition"/> is <see langword="false"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(bool condition, string message, Object context = null)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(message, formatter.ColorizePlainText(message), LogType.Assert, 0, 0, context, ShouldHideMessage(message) ? stackTrace : null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> throws an <see cref="Exception">exception</see> of type <typeparamref name="TException"/>.
	/// <para>
	/// If condition is <see langword="true"/> does nothing.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not terminating the process with an error.
	/// </para>
	/// <para>
	/// Note that an exception will be thrown every time that this method is called and <paramref name="condition"/> evaluates to <see langword="false"/>.
	/// This is in contrast to some of the other Guard methods that only log an error once per session.
	/// </para>
	/// <para>
	/// Calls to this method will be full stripped in release builds if build stripping has been enabled in preferences.
	/// If you don't want this behaviour you can use <see cref="Critical.Guard{TException}(bool, object[])"/> instead.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		Debug.Guard<ArgumentNullException>(component != null, nameof(component)));
	///		Debug.Guard<ArgumentNullException>(to != null, nameof(to));
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
    /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void Guard<TException>(bool condition, params object[] exceptionArguments) where TException : Exception, new()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(condition)
        {
			return;
        }

		if(exceptionArguments == null || exceptionArguments.Length == 0)
		{
			throw new TException();
		}
		throw Activator.CreateInstance(typeof(TException), exceptionArguments) as TException;
		#endif
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given <paramref name="channel"/>
	/// an error <paramref name="message"/> and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null && to != null, "CopyComponent called with a null argument."))
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
	/// <param name="message"> Message to display if <paramref name="condition"/> is <see langword="false"/>. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
    /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(int channel, bool condition, string message, Object context = null)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(message, formatter.ColorizePlainText(message), LogType.Assert, channel, 0, context, !channels.IsEnabled(channel) ? stackTrace : null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given <paramref name="channel"/> an error message
	/// formed by joining the given text strings together and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null, Channel.Utils, nameof(CopyComponent), " called with null ", nameof(component), " argument.")
	///			|| Debug.Guard(to != null, Channel.Utils, nameof(CopyComponent), " called with null ", nameof(to), " argument."))
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
	/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
    /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(int channel, bool condition, params string[] messageParts)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Assert, channel, 0, null, !channels.IsEnabled(channel) ? stackTrace : null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error message
	/// formed by joining the given text strings together and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null, nameof(CopyComponent), " called with null ", nameof(component), " argument.")
	///			|| Debug.Guard(to != null, nameof(CopyComponent), " called with null ", nameof(to), " argument."))
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
	/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
    /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(bool condition, params string[] messageParts)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
				LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Assert, 0, 0, null, hide ? stackTrace : null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error message containing the name and value
	/// of <paramref name="classMember">class member</paramref> and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null, ()=>component) || Debug.Guard(to != null, ()=>to))
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
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
	/// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	public static bool Guard(bool condition, [NotNull]Expression<Func<object>> classMember, Object context = null)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(classMember, LogType.Assert, 0, 0, context, null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console on the given <paramref name="channel"/> an error message
	/// containing the name and value of <paramref name="classMember">class member</paramref> and returns <see langword="true"/>.
	/// <para>
	/// If condition is <see langword="true"/> returns <see langword="false"/> without logging anything.
	/// </para>
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
    /// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null, Channel.Utils, ()=>component) || Debug.Guard(to != null, Channel.Utils, ()=>to))
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
	/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
	/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
    /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/>; otherwise, <see langword="false"/>. </returns>
	public static bool Guard(int channel, bool condition, [NotNull]Expression<Func<object>> classMember, Object context = null)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				LogInternal(classMember, LogType.Assert, channel, 0, context, !channels.IsEnabled(channel) ? stackTrace : null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// Logs an error message to the Console if <paramref name="condition"/> is not <see langword="true"/>
	/// and returns <see langword="true"/> if <paramref name="condition"/> was <see langword="false"/> or <see langword="false"/> if it was not.
	/// <para>
	/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
	/// </para>
	/// <para>
	/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
	/// </para>
	/// <para>
	/// In release builds an error will only be logged if the UNITY_ASSERTIONS symbol is defined.
	/// </para>
	/// <example>
	/// <code>
	/// private void CopyComponent(Component component, GameObject to)
	/// {
	///		if(Debug.Guard(component != null && to != null, ()=>CopyComponent(component, to)))
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
	/// <param name="methodContext"> Expression pointing to a method to which the guard applies. </param>
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static bool Guard(bool condition, [NotNull]Expression<Action> methodContext)
	{
		if(condition)
		{
			return false;
		}

		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(IsLogTypeAllowed(LogType.Assert))
		{
			string stackTrace = StackTraceUtility.ExtractStackTrace();
			if(failedGuards.Add(stackTrace))
			{
				var context = ExpressionUtility.GetOwner(methodContext);
				LogInternal(methodContext, LogType.Assert, 0, 0, context, null);
			}
		}
		#endif

		return true;
	}

	/// <summary>
	/// Start displaying the name and value of the class member on screen.
	/// <para>
	/// Value will continue to be displayed until <see cref="CancelDisplayOnScreen"/> is called with an
	/// expression pointing to the same class member.
	/// </para>
	/// </summary>
	/// <param name="classMember"> Expression pointing to the class member to display on screen. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void DisplayOnScreen([NotNull]Expression<Func<object>> classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		foreach(var displayed in DisplayedOnScreen)
		{
			if(displayed is ValueDisplayer valueDisplayer && valueDisplayer.TargetEquals(classMember))
			{
				return;
			}
		}

		var displayer = new ValueDisplayer(classMember, formatter);
		DisplayedOnScreen.Add(displayer);
		DebuggedEveryFrame.Add(displayer);
		#endif
	}

	/// <summary>
	/// Gets a value indicating whether or not the name and value of the class member in question
	/// is currently being displayed on screen.
	/// </summary>
	/// <param name="classMember"> The class member to check. </param>
	/// <returns> <see langword="true"/> if member is being displayed on screen; otherwise, <see langword="false"/>. </returns>
	public static bool IsDisplayedOnScreen(MemberInfo classMember)
	{
		foreach(var displayed in DisplayedOnScreen)
		{
			if(displayed is MemberInfoValueDisplayer valueDisplayer && valueDisplayer.TargetEquals(classMember))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Gets a value indicating whether or not changes made to the class member in question are currently being tracked by the Debug class,
	/// whether it is for the purposes of logging changes made to its value or for displaying its value on the screen.
	/// </summary>
	/// <param name="classMember"> The class member to check. </param>
	/// <returns> <see langword="true"/> if member is being tracked; otherwise, <see langword="false"/>. </returns>
	public static bool IsBeingTracked(MemberInfo classMember)
	{
		foreach(var debugged in DebuggedEveryFrame)
		{
			if(debugged is ValueTracker tracker && tracker.TargetEquals(classMember))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Start displaying the name and value of the class member on screen.
	/// <para>
	/// Value will continue to be displayed until <see cref="CancelDisplayOnScreen"/> is called with an
	/// expression pointing to the same class member.
	/// </para>
	/// </summary>
	/// <param name="trackMember"> Expression pointing to the class member to display on screen. </param>
	/// <param name="memberOwner">
	/// Instance of the class that contains the member and from which the value of the member is read.
	/// <para>
	/// This can be <see langword="null"/> if the <see cref="MemberInfo"/> represents a static member.
	/// </para>
	/// </param>
	/// <param name="classMember">
	/// <see cref="MemberInfo"/> representing the class member to display on screen.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void DisplayOnScreen([CanBeNull]object memberOwner, [NotNull]MemberInfo classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		foreach(var displayed in DisplayedOnScreen)
		{
			if(displayed is MemberInfoValueDisplayer memberDisplayer && memberDisplayer.TargetEquals(classMember))
			{
				return;
			}
		}

		var displayer = new MemberInfoValueDisplayer(memberOwner, classMember, formatter);
		DisplayedOnScreen.Add(displayer);
		DebuggedEveryFrame.Add(displayer);
		#endif
	}

	/// <summary>
	/// Stop displaying the name and value of a class member on screen.
	/// </summary>
	/// <param name="classMember"> Expression pointing to a class member that is being displayed on screen. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelDisplayOnScreen([NotNull]Expression<Func<object>> classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		foreach(var displayed in DisplayedOnScreen)
		{
			if(displayed is ValueDisplayer valueDisplayer && valueDisplayer.TargetEquals(classMember))
			{
				DisplayedOnScreen.Remove(displayed);
				DebuggedEveryFrame.Remove(displayed as IUpdatable);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Stop displaying the name and value of a class member on screen.
	/// </summary>
	/// <param name="classMember"> <see cref="MemberInfo"/> representing a class member being displayed on screen. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelDisplayOnScreen([NotNull]MemberInfo classMember)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		foreach(var displayed in DisplayedOnScreen)
		{
			if(displayed is MemberInfoValueDisplayer memberDisplayer && memberDisplayer.TargetEquals(classMember))
			{
				DisplayedOnScreen.Remove(displayed);
				DebuggedEveryFrame.Remove(displayed as IUpdatable);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Start displaying a button on screen which calls a method when clicked.
	/// <para>
	/// Button will continue to be displayed until <see cref="CancelDisplayButton(Expression{Action})"/>
	/// is called with an expression pointing to the same method.
	/// </para>
	/// <example>
	/// <code>
	/// void OnEnable()
	/// {
	///		Debug.DisplayButton(()=>SayHello());
	/// }
	/// 
	/// void SayHello()
	/// {
	///		Debug.Log("Hello!");
	/// }
	/// 
	/// void OnDisable()
	/// {
	///		Debug.CancelDisplayButton(()=>SayHello());
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="onClicked">
	/// Expression pointing to the method to call when the button is clicked.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void DisplayButton([NotNull]Expression<Action> onClicked)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		for(int i = DisplayedOnScreen.Count - 1; i >= 0; i--)
		{
			if(DisplayedOnScreen[i] is ButtonDisplayer buttonDisplayer && buttonDisplayer.TargetEquals(onClicked))
			{
				return;
			}
		}

		DisplayedOnScreen.Add(new ButtonDisplayer(onClicked));
		#endif
	}

	/// <summary>
	/// Start displaying a button on screen which calls a method when clicked.
	/// <para>
	/// Button will continue to be displayed until <see cref="CancelDisplayButton(string)"/>
	/// is called with the same label.
	/// </para>
	/// <example>
	/// <code>
	/// void OnEnable()
	/// {
	///		Debug.DisplayButton("Say Hello", ()=>Debug.Log("Hello!"));
	/// }
	/// 
	/// void OnDisable()
	/// {
	///		Debug.CancelDisplayButton("Say Hello");
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="label"> Label to display on the button. </param>
	/// <param name="onClicked">
	/// Method to call when the button is clicked.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void DisplayButton(string label, [NotNull]Action onClicked)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(!IsLogTypeAllowed(LogType.Log))
		{
			return;
		}

		for(int i = DisplayedOnScreen.Count - 1; i >= 0; i--)
		{
			if(DisplayedOnScreen[i] is ButtonDisplayer buttonDisplayer && buttonDisplayer.TargetEquals(onClicked))
			{
				return;
			}
		}

		DisplayedOnScreen.Add(new ButtonDisplayer(label, onClicked));
		#endif
	}

	/// <summary>
	/// Stop displaying a button on screen.
	/// <example>
	/// <code>
	/// void OnEnable()
	/// {
	///		Debug.DisplayButton(()=>SayHello());
	/// }
	/// 
	/// void SayHello()
	/// {
	///		Debug.Log("Hello!");
	/// }
	/// 
	/// void OnDisable()
	/// {
	///		Debug.CancelDisplayButton(()=>SayHello());
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="onClicked">
	/// Expression pointing to a method that is being displayed on screen as a button.
	/// </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelDisplayButton([NotNull]Expression<Action> onClicked)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		for(int i = DisplayedOnScreen.Count - 1; i >= 0; i--)
		{
			if(DisplayedOnScreen[i] is ButtonDisplayer buttonDisplayer && buttonDisplayer.TargetEquals(onClicked))
			{
				DisplayedOnScreen.RemoveAt(i);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Stop displaying a button on screen.
	/// <example>
	/// <code>
	/// void OnEnable()
	/// {
	///		Debug.DisplayButton("Say Hello", ()=>Debug.Log("Hello!"));
	/// }
	/// 
	/// void OnDisable()
	/// {
	///		Debug.CancelDisplayButton("Say Hello");
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="label"> Label of a button that is being displayed on screen. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelDisplayButton([NotNull]string label)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		for(int i = DisplayedOnScreen.Count - 1; i >= 0; i--)
		{
			if(DisplayedOnScreen[i] is ButtonDisplayer buttonDisplayer && buttonDisplayer.TargetEquals(label))
			{
				DisplayedOnScreen.RemoveAt(i);
				return;
			}
		}
		#endif
	}

	/// <summary>
	/// Start displaying frame rate on screen.
	/// <para>
	/// Frame rate will continue to be displayed until <see cref="CancelShowFps"/> is called.
	/// </para>
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void ShowFps()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		DisplayOnScreen(()=>FpsCounter.Fps);
		#endif
	}

	/// <summary>
	/// Stop displaying frame rate on screen.
	/// <para>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void CancelShowFps()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		CancelDisplayOnScreen(() => FpsCounter.Fps);
		#endif
	}

	/// <summary>
	/// Clears the screen from all GUI elements that have been added to it using <see cref="DisplayOnScreen"/>.
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void ClearDisplayedOnScreen()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		foreach(var displayed in DisplayedOnScreen)
		{
			var updated = displayed as IUpdatable;
			if(updated != null)
			{
				DebuggedEveryFrame.Remove(updated);
			}
		}
		DisplayedOnScreen.Clear();
		#endif
	}

	/// <summary>
	/// Enables logging of messages on the given <paramref name="channel">channel</paramref>.
	/// </summary>
	/// <param name="channel"> Name of the channel to enable. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void EnableChannel([NotNull]string channel)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG

		#if DEV_MODE
		UnityEngine.Debug.Assert(!string.IsNullOrEmpty(channel));
		UnityEngine.Debug.Assert(channel.Length == 0 || channel[0] != '[');
		UnityEngine.Debug.Assert(channel.Trim() == channel);
		#endif

		channels.EnableChannel(channel);
		#endif
	}

	/// <summary>
	/// Disables logging of messages on the given <paramref name="channel">channel</paramref>.
	/// </summary>
	/// <param name="channel"> Name of the channel to disable. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void DisableChannel([NotNull]string channel)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG

		#if DEV_MODE
		UnityEngine.Debug.Assert(!string.IsNullOrEmpty(channel));
		UnityEngine.Debug.Assert(channel.Length == 0 || channel[0] != '[');
		UnityEngine.Debug.Assert(channel.Trim() == channel);
		#endif

		channels.DisableChannel(channel);
		#endif
	}

	/// <summary>
	/// Starts a new stopwatch counting upwards from zero.
	/// <para>
	/// The stopwatch will continue running until <see cref="FinishStopwatch"/> is called.
	/// </para>
	/// <example>
	/// <code>
	/// void Start()
	/// {
	///		// Prints:
	///		// Stopwatch 1 . . . 3.2 s
	///		LoadLevel("Test");
	/// }
	/// 
	/// void LoadLevel(string sceneName)
	/// {
	/// 	Debug.StartStopwatch();
	/// 	
	/// 	SceneManager.LoadScene(sceneName);
	/// 	
	/// 	Debug.FinishStopwatch();
	/// }
	/// </code>
	/// </example>
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void StartStopwatch()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		string name;
		if(stopwatches.Count == 0)
		{
			nextStopwatchNameIndex = 0;
			name = stopwatchNames[0];
		}
		else
		{
			do
			{
				nextStopwatchNameIndex++;
				if(stopwatchNames.Length > nextStopwatchNameIndex)
				{
					name = stopwatchNames[nextStopwatchNameIndex];
				}
				else
				{
					name = "Stopwatch " + (nextStopwatchNameIndex + 1);
				}
			}
			while(stopwatches.FindIndex((item)=>item.name == name) != -1);
		}
		stopwatches.Add(new NestableStopwatch(name));
		#endif
	}

	/// <summary>
	/// Starts a new stopwatch counting upwards from zero with the given name.
	/// <para>
	/// The stopwatch will continue running until <see cref="FinishStopwatch(string)"/> is called with the same name.
	/// </para>
	/// <para>
	/// If a stopwatch by the same name already exists a warning will be logged and no new stopwatch will be started.
	/// </para>
	/// <example>
	/// <code>
	/// void Start()
	/// {
	///		// Prints:
	///		// LoadLevel : Test . . . 14.672 s
	///		LoadLevel("Test");
	/// }
	/// 
	/// void LoadLevel(string sceneName)
	/// {
	/// 	Debug.StartStopwatch($"{nameof(LoadLevel)} : {sceneName}");
	/// 
	/// 	SceneManager.LoadScene(sceneName);
	/// 
	/// 	Debug.FinishStopwatch();
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="name"> The name for the stopwatch. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void StartStopwatch([NotNull]string name)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(stopwatches.FindIndex((item)=>item.name == name) != -1)
		{
			LogWarning("StartStopwatch(\"" + name + "\") called but stopwatch by name was already running");
			return;
		}
		stopwatches.Add(new NestableStopwatch(name));
		#endif
	}

	/// <summary>
	/// Starts a new sub-stopwatch running under the stopwatch that was last started using <see cref="StartStopwatch"/>.
	/// <para>
	/// The sub-stopwatch will continue running until <see cref="FinishSubStopwatch()"/> is called.
	/// </para>
	/// <example>
	/// <code>
	/// void Start()
	/// {
	///		// Prints:
	///		// Stopwatch 1 . . . 0.001 s
	///		//   Sub Stopwatch 1 . . . 0 s
	/// }
	/// 
	/// void LoadLevel(string sceneName)
	/// {
	/// 	Debug.StartStopwatch();
	/// 
	///		SceneManager.LoadScene(sceneName);
	/// 
	/// 	Debug.StartSubStopwatch();
	/// 
	/// 	LoadSubscenes(sceneName);
	/// 	
	///		Debug.FinishSubStopwatch();
	/// 
	/// 	Debug.FinishStopwatch();
	/// }
	/// </code>
	/// </example>
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void StartSubStopwatch()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		int count = stopwatches.Count;
		if(count == 0)
        {
			LogWarning("StartSubStopwatch() was called but no stopwatches were running.");
			return;
        }
		stopwatches[count - 1].StartSubStopwatch();
		#endif
	}


	/// <summary>
	/// Starts a new sub-stopwatch running under the stopwatch that was last started using <see cref="StartStopwatch"/>.
	/// <para>
	/// The sub-stopwatch will continue running until <see cref="FinishSubStopwatch(string)"/> is called with the same name.
	/// </para>
	/// <para>
	/// If no stopwatches are currently running a warning will be logged and no sub-stopwatch will be started.
	/// </para>
	/// <example>
	/// <code>
	/// void Start()
	/// {
	///		// Prints:
	///		// LoadLevel : Test . . . 14.672 s
	///		//    Environment . . . 9.671 s
	///		//       Terrain . . . 0.669 s
	///		//       Trees . . . 1.999 s
	///		//       Vegetation . . . 3 s
	///		//    Actors . . . 5 s
	///		LoadLevel("Test");
	/// }
	/// 
	/// void LoadLevel(string sceneName)
	/// {
	/// 	Debug.StartStopwatch($"{nameof(LoadLevel)} : {sceneName}");
	/// 
	/// 	SceneManager.LoadScene(sceneName);
	/// 	Load(LoadEnvironment);
	/// 	Load(LoadActors);
	/// 
	/// 	Debug.FinishStopwatch();
	/// }
	/// 
	/// void LoadEnvironment()
	/// {
	/// 	Load(LoadTerrain);	
	/// 	Load(LoadTrees);
	/// 	Load(LoadVegetation);
	/// }
	/// 
	/// void Load(Action operation)
	/// {
	/// 	Debug.StartSubStopwatch(operation.Method.Name);
	/// 
	/// 	operation();
	/// 
	/// 	Debug.FinishSubStopwatch();
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="name"> The label for the timer. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void StartSubStopwatch([NotNull]string name)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		int count = stopwatches.Count;
		if(count == 0)
        {
			LogWarning("StartSubStopwatch(\""+name+"\") was called but no stopwatches were running.");
			return;
        }
		stopwatches[count - 1].StartSubStopwatch(name);
		#endif
	}

	/// <summary>
	/// Starts a new sub-stopwatch running under a parent stopwatch.
	/// If main stopwatch by provided name does not exist yet one will be created.
	/// <example>
	/// <code>
	/// void Start()
	/// {
	///		// Prints:
	///		// LoadLevel : Test . . . 14.672 s
	///		//    Environment . . . 9.671 s
	///		//       Terrain . . . 0.669 s
	///		//       Trees . . . 1.999 s
	///		//       Vegetation . . . 3 s
	///		//    Actors . . . 5 s
	///		LoadLevel("Test");
	/// }
	/// 
	/// void LoadLevel(string sceneName)
	/// {
	/// 	Debug.StartStopwatch($"{nameof(LoadLevel)} : {sceneName}");
	/// 
	/// 	SceneManager.LoadScene(sceneName);
	/// 	Load(LoadEnvironment);
	/// 	Load(LoadActors);
	/// 
	/// 	Debug.FinishStopwatch();
	/// }
	/// 
	/// void LoadEnvironment()
	/// {
	/// 	Load(LoadTerrain);	
	/// 	Load(LoadTrees);
	/// 	Load(LoadVegetation);
	/// }
	/// 
	/// void Load(Action operation)
	/// {
	/// 	Debug.StartSubStopwatch(operation.Method.Name);
	/// 
	/// 	operation();
	/// 
	/// 	Debug.FinishSubStopwatch();
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="parentName"> Name of parent stopwatch under which the sub-stopwatch will be nested. </param>
	/// <param name="name"> Name of new sub-stopwatch to start. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void StartSubStopwatch([NotNull]string parentName, [NotNull]string name)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		NestableStopwatch parent;
		if(!TryFindStopwatch(parentName, out parent))
		{
			parent = new NestableStopwatch(parentName);
			stopwatches.Add(parent);
		}
		parent.StartSubStopwatch(name);
		#endif
	}

	#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
	private static bool TryFindStopwatch(string name, out NestableStopwatch result)
    {
		int count = stopwatches.Count;
		if(count == 0)
        {
			result = null;
			return false;
        }

		for(int i = count - 1; i >= 0; i--)
        {
			if(string.Equals(stopwatches[i].name, name))
            {
				result = stopwatches[i];
				return true;
			}
        }

		for(int i = count - 1; i >= 0; i--)
		{
			if(stopwatches[i].TryFindStopwatchInChildren(name, out result))
            {
				return true;
            }
		}

		result = null;
		return false;
    }
	#endif

	/// <summary>
	/// Gets the last started stopwatch and finishes the sub-stopwatch inside it which was last started,
	/// still leaving the main stopwatch running.
	/// <para>
	/// Results are not logged at this point, only when you finish the main stopwatch.
	/// </para>
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void FinishSubStopwatch()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		int count = stopwatches.Count;
		if(count == 0)
        {
			LogWarning("FinishSubStopwatch() was called but no stopwatches were running.");
			return;
        }
		stopwatches[count - 1].FinishSubStopwatch();
		#endif
	}

	/// <summary>
	/// Finishes a previously created sub-stopwatch, still leaving the main stopwatch running.
	/// Results are not logged at this point, only when you finish the main stopwatch.
	/// </summary>
	/// <param name="parentName"> Name of parent stopwatch which contains the sub-stopwatch to stop. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	public static void FinishSubStopwatch([NotNull]string parentName)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		NestableStopwatch parent;
		if(!TryFindStopwatch(parentName, out parent))
        {
			LogWarning("FinishSubStopwatch(\"" + parentName + "\") was called but parent stopwatch by the given name was not found.");
			return;
        }
		parent.FinishSubStopwatch();
		#endif
	}

	/// <summary>
	/// Finishes a previously created sub-stopwatch, still leaving the main stopwatch running.
	/// Results are not logged at this point, only when you finish the main stopwatch.
	/// </summary>
	/// <param name="parentName"> Name of parent stopwatch which contains the sub-stopwatch to stop. </param>
	/// <param name="name"> Name of sub-stopwatch to stop. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void FinishSubStopwatch([NotNull]string parentName, [NotNull]string name)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		NestableStopwatch parent;
		if(!TryFindStopwatch(parentName, out parent))
        {
			LogWarning("FinishSubStopwatch(\"" + parentName + "\", \"" + name + "\") was called but parent stopwatch by the given name was not found.");
			return;
        }
		parent.FinishSubStopwatch(name);
		#endif
	}

	/// <summary>
	/// Logs results of the last created stopwatch and clears it.
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void FinishStopwatch()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		int count = stopwatches.Count;
		if(count == 0)
        {
			LogWarning("FinishStopwatch() was called but no stopwatches were running.");
			return;
        }
		int index = count - 1;
		var stopwatch = stopwatches[index];
		stopwatches.RemoveAt(index);
		stopwatch.FinishAndLogResults(formatter);
		#endif
	}

	/// <summary>
	/// Logs results of a previously created stopwatch and then clears it.
	/// </summary>
	/// <param name="name"> Name of stopwatch to stop. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void FinishStopwatch([NotNull]string name)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		NestableStopwatch stopwatch;
		if(!TryFindStopwatch(name, out stopwatch))
		{
			LogWarning("FinishStopwatch(\"" + name+"\") was called but no stopwatch by the given name were running.");
			return;
		}
		stopwatch.FinishAndLogResults(formatter);
		stopwatches.Remove(stopwatch);
		#endif
	}

	/// <summary>
	/// Logs results of a all previously created stopwatches and the clears them.
	/// </summary>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void FinishAllStopwatches()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		for(int i = stopwatches.Count - 1; i >= 0; i--)
        {
			stopwatches[i].FinishAndLogResults(formatter);
		}
		stopwatches.Clear();
		#endif
	}

	internal static bool ShouldHideMessage(object message) => ShouldHideMessage(message as string);

	/// <summary>
	/// If message contains any tags of channels that are enabled, returns false.
	/// Otherwise if message contains any tags of channels that are disabled, returns true.
	/// Otherwise returns false;
	/// </summary>
	/// <param name="message"> Message which might contain channel prefixes. </param>
	/// <returns> <see langword="true"/> if message should be hidden at this time, otherwise, <see langword="false"/>. </returns>
	internal static bool ShouldHideMessage(string message)
	{
		if(message == null || !formatter.StartsWithChannelPrefix(message))
		{
			return false;
		}

		bool hide = false;

		for(int from = 1, to = message.IndexOf(']', 1), count = message.Length; to != -1; to = message.IndexOf(']', from + 1))
		{
			string channel = message.Substring(from, to - from);
			if(channels.IsEnabled(channel))
			{
				return false;
			}
			hide = true;

			from = to + 1;
			if(from >= count || message[from] != '[')
			{
				return hide;
			}
			from++;
		}

		return hide;
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	internal static void Update()
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		foreach(var debugged in DebuggedEveryFrame)
		{
			debugged.Update();
		}
		FpsCounter.Update();
		#endif
	}

	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static bool IsLogTypeAllowed(LogType logType)
	{
		return usePlayerLog && UnityEngine.Debug.unityLogger.logEnabled && UnityEngine.Debug.unityLogger.IsLogTypeAllowed(logType);
	}

    internal static void BroadcastLogMessageSuppressed(string uncolorized, string colorized, string stackTrace, LogType type, Object context)
    {
        if(LogMessageSuppressed == null)
        {
            return;
        }
        LogMessageSuppressed(uncolorized, formatter.Format(colorized), stackTrace, type, context);
    }

	internal static void BroadcastLogMessageSuppressed(object message, string stackTrace, LogType type, Object context)
	{
		if(LogMessageSuppressed == null)
		{
			return;
		}

		string textUnformatted = message as string;
		if(textUnformatted != null)
		{
			LogMessageSuppressed(textUnformatted, formatter.ColorizePlainText(textUnformatted), stackTrace, type, context);
			return;
		}

		string textFormatted;
		var classMember = message as Expression<Func<object>>;
		if(classMember != null)
		{
			formatter.ToString(classMember, out textUnformatted, out textFormatted);

			if(context == null)
			{
				context = ExpressionUtility.GetContext(classMember);
			}

			LogMessageSuppressed(textUnformatted, textFormatted, stackTrace, type, context);
			return;
		}

		var method = message as Expression<Action>;
		if(method != null)
        {
			textUnformatted = ExpressionUtility.TargetToString(method);

			if(context == null)
			{
				context = ExpressionUtility.GetOwner(method);
			}

			LogMessageSuppressed(textUnformatted, formatter.ColorizePlainText(textUnformatted), stackTrace, type, context);
			return;
		}

		if(context == null)
        {
			context = message as Object;
        }

		textUnformatted = formatter.ToStringUncolorized(message, false);
		textFormatted = formatter.ToStringColorized(message, false);

		LogMessageSuppressed(textUnformatted, formatter.Format(textFormatted), stackTrace, type, context);
	}

	internal static void BroadcastLogMessageSuppressed(object message, string stackTrace, LogType type, int channel1, int channel2, Object context)
	{
		if(LogMessageSuppressed == null)
		{
			return;
		}

		string textUnformatted = message as string;
		if(textUnformatted != null)
		{
			LogMessageSuppressed(formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted),
					formatter.Format(formatter.ColorizePlainText(channel1, channel2, textUnformatted)), stackTrace, type, context);
			return;
		}

		string textFormatted;
		var classMember = message as Expression<Func<object>>;
		if(classMember != null)
		{
			formatter.ToString(classMember, out textUnformatted, out textFormatted);

			if(context == null)
			{
				context = ExpressionUtility.GetContext(classMember);
			}

			LogMessageSuppressed(formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted),
				formatter.Format(formatter.WithColorizedPrefixes(channel1, channel2, textFormatted)), stackTrace, type, context);
			return;
		}

		var method = message as Expression<Action>;
		if(method != null)
        {
			textUnformatted = ExpressionUtility.TargetToString(method);

			if(context == null)
			{
				context = ExpressionUtility.GetOwner(method);
			}

			LogMessageSuppressed(formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted),
				formatter.Format(formatter.ColorizePlainText(channel1, channel2, textUnformatted)), stackTrace, type, context);
			return;
		}

		if(context == null)
        {
			context = message as Object;
        }

		textUnformatted = formatter.ToStringUncolorized(message, false);
		textFormatted = formatter.ToStringColorized(message, false);

		LogMessageSuppressed(formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted),
			formatter.Format(formatter.WithColorizedPrefixes(channel1, channel2, textFormatted)), stackTrace, type, context);
	}

	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void BroadcastLogMessageSuppressed(string unformatted, string stackTrace, LogType type, int channel, Object context)
	{
		if(LogMessageSuppressed == null)
		{
			return;
		}

		string formatted;
		if(channel != 0)
		{
			var sb = new StringBuilder();

			formatter.ColorizePlainTextWithPrefix(channel, unformatted, sb);
			formatted = sb.ToString();
			sb.Length = 0;

			formatter.WithUncolorizedPrefix(channel, unformatted, sb);
			unformatted = sb.ToString();
			sb.Length = 0;
		}
		else
        {
			formatted = formatter.ColorizePlainText(unformatted);
		}

		LogMessageSuppressed(unformatted, formatted, stackTrace, type, context);
	}

	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void BroadcastLogMessageSuppressed(string unformatted, string formatted, string stackTrace, LogType type, int channel1, int channel2, Object context)
	{
		if(LogMessageSuppressed == null)
		{
			return;
		}

		if(channel1 != 0)
		{
			var sb = new StringBuilder();

			formatter.WithColorizedPrefixes(channel1, channel2, formatted, sb);
			formatted = sb.ToString();
			sb.Length = 0;

			formatter.WithUncolorizedPrefixes(channel1, channel2, unformatted, sb);
			unformatted = sb.ToString();
		}

		LogMessageSuppressed(unformatted, formatted, stackTrace, type, context);
	}

	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void BroadcastLogMessageSuppressed(string unformatted, string formatted, string stackTrace, LogType type, int channel, Object context)
	{
		if(LogMessageSuppressed == null)
		{
			return;
		}

		if(channel != 0)
		{
			var sb = new StringBuilder();
			formatter.WithUncolorizedPrefix(channel, unformatted, sb);
			unformatted = sb.ToString();
			sb.Length = 0;

			formatter.WithColorizedPrefix(channel, formatted, sb);
			formatted = sb.ToString();
		}

		LogMessageSuppressed(unformatted, formatted, stackTrace, type, context);
	}

	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogStateInternal([NotNull]Type classType, [CanBeNull]object target, BindingFlags flags, int channel1, int channel2, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		var unformatted = new StringBuilder();
		var formatted = new StringBuilder();

		if(channel1 != 0)
		{
			formatter.AppendPrefixUncolorized(channel1, unformatted);
			formatter.AppendPrefixColorized(channel1, formatted);
			if(channel2 != 0)
			{
				formatter.AppendPrefixUncolorized(channel2, unformatted);
				formatter.AppendPrefixColorized(channel2, formatted);
			}
			unformatted.Append(' ');
			formatted.Append(' ');
		}

		string typeName = classType.Name;
		unformatted.Append(typeName);
		formatted.Append(typeName);

		unformatted.Append(NameStateSeparator);
		formatted.Append(NameStateSeparator);

		var type = classType;
		do
		{
			var fields = type.GetFields(flags);
			for(int n = 0, count = fields.Length; n < count; n++)
			{
				var field = fields[n];
				if(field.Name[0] == '<') //skip property backing fields
				{
					continue;
				}

				unformatted.Append(Environment.NewLine);
				formatted.Append(Environment.NewLine);

				formatter.ToStringUncolorized(target, field, unformatted);
				formatter.ToStringColorized(target, field, formatted);
			}

			var properties = type.GetProperties(flags);
			for(int n = 0, count = properties.Length; n < count; n++)
			{
				var property = properties[n];
				if(!property.CanRead)
				{
					continue;
				}
				unformatted.Append(Environment.NewLine);
				formatted.Append(Environment.NewLine);

				formatter.ToStringUncolorized(target, property, unformatted);
				formatter.ToStringColorized(target, property, formatted);
			}

			if((flags & BindingFlags.DeclaredOnly) == 0)
			{
				break;
			}

			type = type.BaseType;
		}
		// avoid obsolete warnings and excessive number of results by skipping base types such as Component
		while(type != null && type != typeof(object) && (type.Namespace == null || (!string.Equals(type.Namespace, "UnityEngine", StringComparison.Ordinal) && !string.Equals(type.Namespace, "UnityEditor", StringComparison.Ordinal))));

		if(unformatted.Length <= formatter.maxLengthBeforeLineSplitting)
		{
			unformatted.Replace(NameStateSeparator + Environment.NewLine, NameStateSeparator);
			unformatted.Replace(Environment.NewLine, formatter.MultipleEntrySeparator);
			
			formatted.Replace(NameStateSeparator + Environment.NewLine, NameStateSeparator);
			formatted.Replace(Environment.NewLine, formatter.MultipleEntrySeparator);
		}

		formatter.Format(formatted);

		var context = target as Object;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(unformatted.ToString(), formatted.ToString(), stackTraceIfHidden, LogType.Log, context);
			return;
        }

		LastMessageUnformatted = unformatted.ToString();
		LastMessageContext = context;

		UnityEngine.Debug.Log(formatted.ToString(), context);
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal(object messagePrefixOrArg, object arg, object[] args, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		var unformatted = new StringBuilder();
		var formatted = new StringBuilder();

		if(context == null)
		{
			context = messagePrefixOrArg as Object;
		}

		if(channel1 != 0)
		{
			formatter.AppendPrefixUncolorized(channel1, unformatted);
			formatter.AppendPrefixColorized(channel1, formatted);
			if(channel2 != 0)
            {
				formatter.AppendPrefixUncolorized(channel2, unformatted);
				formatter.AppendPrefixColorized(channel2, formatted);
			}
			unformatted.Append(' ');
			formatted.Append(' ');
		}

		string textPrefix = messagePrefixOrArg as string;
		if(!ReferenceEquals(textPrefix, null))
		{
			if(textPrefix.Length > 0)
			{
				unformatted.Append(textPrefix);
				formatted.Append(formatter.ColorizePlainText(textPrefix));
			}
		}
		else if(ReferenceEquals(messagePrefixOrArg, null))
		{
			unformatted.Append(formatter.NullUncolorized);
			formatted.Append(Environment.NewLine);
			formatted.Append(formatter.Null);
			formatted.Append(Environment.NewLine);
		}
		else
		{
			formatter.ToStringUncolorized(messagePrefixOrArg, unformatted, false);
			unformatted.Append(Environment.NewLine);
			formatter.ToStringColorized(messagePrefixOrArg, formatted, false);
			formatted.Append(Environment.NewLine);
		}

		if(context == null)
		{
			context = arg as Object;
		}

		formatter.ToStringUncolorized(arg, unformatted, false);
		formatter.ToStringColorized(arg, formatted, false);

		int count = args == null ? 0 : args.Length;
		if(count > 0)
		{
			for(int n = 0; n < count; n++)
			{
				arg = args[n];
				formatted.Append(Environment.NewLine);
				formatter.ToStringColorized(arg, formatted, false);
				unformatted.Append(Environment.NewLine);
				formatter.ToStringUncolorized(arg, unformatted, false);

				if(context == null)
				{
					context = arg as Object;
				}
			}
		}

		if(unformatted.Length <= formatter.maxLengthBeforeLineSplitting)
		{
			formatted.Replace(Environment.NewLine, formatter.MultipleEntrySeparator);
			unformatted.Replace(Environment.NewLine, formatter.MultipleEntrySeparatorUnformatted);
		}

		string textUnformatted = unformatted.ToString();

		formatter.Format(formatted);
		string textFormatted = formatted.ToString();

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(textUnformatted, textFormatted, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(textFormatted, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, textFormatted, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(textFormatted, context);
				return;
			default:
				UnityEngine.Debug.Log(textFormatted, context);
				return;
		}
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal([NotNull] Expression<Func<object>> classMember, [CanBeNull]Expression<Func<object>>[] classMembers, LogType type, int channel1, int channel2, Object context, [CanBeNull]string stackTraceIfHidden)
    {
		int count = classMembers == null ? 0 : classMembers.Length;
		var joinedClassMembers = new Expression<Func<object>>[count + 1];
		joinedClassMembers[0] = classMember;
		for(int i = 1; i <= count; i++)
        {
			joinedClassMembers[i + 1] = classMembers[i];
        }
		LogInternal(joinedClassMembers, type, channel1, channel2, context, stackTraceIfHidden);
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal([CanBeNull]Expression<Func<object>>[] classMembers, LogType type, int channel1, int channel2, Object context, [CanBeNull]string stackTraceIfHidden)
    {
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(context == null && classMembers != null && classMembers.Length > 0)
		{
			context = ExpressionUtility.GetContext(classMembers[0]);
		}

		var sb = new StringBuilder(128);
		if(channel1 != 0)
		{
			formatter.AppendPrefixUncolorized(channel1, sb);
			if(channel2 != 0)
            {
				formatter.AppendPrefixUncolorized(channel2, sb);
			}
			sb.Append(' ');
		}
		formatter.JoinUncolorizedWithSeparatorCharacter(classMembers, sb);
		string textUnformatted = sb.ToString();
		sb.Length = 0;

		if(channel1 != 0)
		{
			formatter.AppendPrefixColorized(channel1, sb);
			if(channel2 != 0)
			{
				formatter.AppendPrefixColorized(channel2, sb);
			}
			sb.Append(' ');
		}
		formatter.JoinColorizedWithSeparatorCharacter(classMembers, sb);
		string textFormatted = sb.ToString();

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(textUnformatted, textFormatted, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(textFormatted, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, textFormatted, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(textFormatted, context);
				return;
			default:
				UnityEngine.Debug.Log(textFormatted, context);
				return;
		}
		#endif
    }

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogFormatInternal(string format, object[] args, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(context == null)
        {
			context = GetFirstUnityObjectOrDefault(args);
        }

		string uncolorized = formatter.FormatUncolorized(format, args);
		string colorized = formatter.FormatColorized(format, args);

		if(channel1 != 0)
		{
			var sb = new StringBuilder();
			formatter.WithColorizedPrefixes(channel1, channel2, colorized, sb);
			formatter.Format(sb);
			colorized = sb.ToString();
			sb.Length = 0;

			formatter.WithUncolorizedPrefixes(channel1, channel2, uncolorized, sb);
			formatter.Format(sb);
			uncolorized = sb.ToString();
		}
		else
        {
			uncolorized = formatter.Format(uncolorized);
			colorized = formatter.Format(colorized);
		}

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(uncolorized, colorized, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(colorized, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, colorized, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(colorized, context);
				return;
			default:
				UnityEngine.Debug.Log(colorized, context);
				return;
		}
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal(string prefix, Expression<Func<object>>[] classMembers, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(context == null)
        {
			context = GetFirstUnityObjectOrDefault(classMembers);
        }

		string uncolorized = formatter.JoinUncolorized(prefix, classMembers);
		string colorized = formatter.JoinColorized(prefix, classMembers);

		if(channel1 != 0)
		{
			var sb = new StringBuilder();
			formatter.WithColorizedPrefixes(channel1, channel2, colorized, sb);
			formatter.Format(sb);
			colorized = sb.ToString();
			sb.Length = 0;

			formatter.WithUncolorizedPrefixes(channel1, channel2, uncolorized, sb);
			formatter.Format(sb);
			uncolorized = sb.ToString();
		}
		else
        {
			uncolorized = formatter.Format(uncolorized);
			colorized = formatter.Format(colorized);
		}

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(uncolorized, colorized, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(colorized, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, colorized, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(colorized, context);
				return;
			default:
				UnityEngine.Debug.Log(colorized, context);
				return;
		}
		#endif
	}

	/// <summary>
	/// <see cref="UnityEngine.Debug.Log(object)">Logs</see> the full <see cref="DebugFormatter.GetFullHierarchyPath">hierarchy path</see> of the given <paramref name="transform"/> to the Console.
	/// </summary>
	/// <param name="transform"> The <see cref="Transform"/> component of a <see cref="GameObject"/> whose hierarchy path to log. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogHierarchyPath([CanBeNull]Transform transform)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		UnityEngine.Debug.Log(formatter.GetFullHierarchyPath(transform));
		#endif
	}

	/// <summary>
	/// <see cref="UnityEngine.Debug.Log(object)">Logs</see> the full <see cref="DebugFormatter.GetFullHierarchyPath">hierarchy path</see> of the given <paramref name="transform"/> to the Console.
	/// </summary>
	/// <param name="transform"> The <see cref="GameObject"/> whose hierarchy path to log. </param>
	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void LogHierarchyPath([CanBeNull]GameObject gameObject)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		UnityEngine.Debug.Log(formatter.GetFullHierarchyPath(gameObject == null ? null : gameObject.transform));
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal(string uncolorized, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		
		string colorized;
		if(channel1 != 0)
        {
			colorized = formatter.Format(formatter.ColorizePlainText(channel1, channel2, uncolorized));
			uncolorized = formatter.WithUncolorizedPrefixes(channel1, channel2, uncolorized);
		}
		else
        {
			colorized = formatter.Format(formatter.ColorizePlainText(uncolorized));
        }

		LastMessageUnformatted = uncolorized;
		LastMessageContext = context;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(uncolorized, colorized, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(colorized, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, colorized, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(colorized, context);
				return;
			default:
				UnityEngine.Debug.Log(colorized, context);
				return;
		}
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal(string uncolorized, string colorized, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden, bool skipFormat = true)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		
		if(channel1 != 0)
        {
			colorized = formatter.WithColorizedPrefixes(channel1, channel2, colorized);
			uncolorized = formatter.WithUncolorizedPrefixes(channel1, channel2, uncolorized);
		}

		if(!skipFormat)
		{
			colorized = formatter.Format(colorized);
		}

		LastMessageUnformatted = uncolorized;
		LastMessageContext = context;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(uncolorized, colorized, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(colorized, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, colorized, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(colorized, context);
				return;
			default:
				UnityEngine.Debug.Log(colorized, context);
				return;
		}
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	private static void LogInternal(string uncolorized, string colorized, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(channel1 != 0)
		{
			var sb = new StringBuilder();

			formatter.WithColorizedPrefixes(channel1, channel2, colorized, sb);
			formatter.Format(sb);
			colorized = sb.ToString();
			sb.Length = 0;

			formatter.WithUncolorizedPrefixes(channel1, channel2, uncolorized, sb);
			uncolorized = sb.ToString();
		}

		LastMessageUnformatted = uncolorized;
		LastMessageContext = context;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(uncolorized, colorized, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(colorized, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, colorized, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(colorized, context);
				return;
			default:
				UnityEngine.Debug.Log(colorized, context);
				return;
		}
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal(object message, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		string textFormatted;
		string textUnformatted = message as string;
		if(textUnformatted != null)
		{
			if(channel1 != 0)
			{
				var sb = new StringBuilder();
				formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted, sb);
				textUnformatted = sb.ToString();
				sb.Length = 0;

				formatter.ColorizePlainTextWithPrefixes(channel1, channel2, textUnformatted, sb);
				textFormatted = sb.ToString();
			}
			else
            {
				textFormatted = formatter.ColorizePlainText(textUnformatted);
			}
		}
		else
		{
			var classMember = message as Expression<Func<object>>;
			if(classMember != null)
			{
				formatter.ToString(classMember, out textUnformatted, out textFormatted);
				if(context == null)
				{
					context = ExpressionUtility.GetContext(classMember);
				}
			}
			else
			{
				var method = message as Expression<Action>;
				if(method != null)
				{
					textUnformatted = ExpressionUtility.TargetToString(method);
					textFormatted = formatter.ColorizePlainText(textUnformatted);
					if(context == null)
					{
						context = ExpressionUtility.GetOwner(method);
					}
				}
				else
				{
					if(context == null)
					{
						context = message as Object;
					}
					textUnformatted = formatter.ToStringUncolorized(message, false);
					textFormatted = formatter.ToStringColorized(message, false);
				}
			}

			if(channel1 != 0)
			{
				var sb = new StringBuilder();

				formatter.WithColorizedPrefixes(channel1, channel2, textFormatted, sb);
				formatter.Format(sb);
				textFormatted = sb.ToString();
				sb.Length = 0;

				formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted, sb);
				textUnformatted = sb.ToString();
			}
			else
            {
				textFormatted = formatter.Format(textFormatted);
            }
		}

		LastMessageUnformatted = textUnformatted;
		LastMessageContext = context;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(textUnformatted, textFormatted, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Exception:
				Exception exception = message as Exception;
				if(exception != null)
                {
					UnityEngine.Debug.LogException(exception, context);
					return;
				}
				UnityEngine.Debug.LogError(textFormatted, context);
				return;
			case LogType.Error:
				UnityEngine.Debug.LogError(textFormatted, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, textFormatted, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(textFormatted, context);
				return;
			default:
				UnityEngine.Debug.Log(textFormatted, context);
				return;
		}
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	internal static void LogInternal(Exception exception, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG

		string textUnformatted = exception.ToString();

		LastMessageUnformatted = textUnformatted;
		LastMessageContext = context;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(textUnformatted, textUnformatted, stackTraceIfHidden, LogType.Exception, context);
			return;
        }

		UnityEngine.Debug.LogException(exception, context);
		#endif
	}

	#if DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED && !DEBUG
	[Conditional("FALSE")]
	#endif
	#if UNITY_EDITOR
	[MethodImpl(AggressiveInlining)]
	#endif
	private static void LogInternal(object message, [NotNull]Expression<Action> contextMethod, LogType type, int channel1, int channel2, [CanBeNull]Object context, [CanBeNull]string stackTraceIfHidden)
	{
		#if !DEBUG_LOG_EXTENSIONS_BUILD_STRIPPING_ENABLED || DEBUG
		if(context == null)
        {
			context = ExpressionUtility.GetOwner(contextMethod);
        }

		var sb = new StringBuilder(32);
		string textFormatted;
		string textUnformatted = message as string;
		if(textUnformatted != null)
		{
			if(channel1 != 0)
			{
				formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted, sb);
				textUnformatted = sb.ToString();
				sb.Length = 0;

				formatter.ColorizePlainTextWithPrefixes(channel1, channel2, textUnformatted, sb);
				textFormatted = sb.ToString();
				sb.Length = 0;
			}
			else
            {
				textFormatted = formatter.ColorizePlainText(textUnformatted);
			}
		}
		else
		{
			var classMember = message as Expression<Func<object>>;
			if(classMember != null)
			{
				formatter.ToString(classMember, out textUnformatted, out textFormatted);
				if(context == null)
				{
					context = ExpressionUtility.GetContext(classMember);
				}
			}
			else
			{
				var method = message as Expression<Action>;
				if(method != null)
				{
					textUnformatted = ExpressionUtility.TargetToString(method);
					textFormatted = formatter.ColorizePlainText(textUnformatted);
					if(context == null)
					{
						context = ExpressionUtility.GetOwner(method);
					}
				}
				else
				{
					if(context == null)
					{
						context = message as Object;
					}
					textUnformatted = formatter.ToStringUncolorized(message, false);
					textFormatted = formatter.ToStringColorized(message, false);
				}
			}

			if(channel1 != 0)
			{
				formatter.WithColorizedPrefixes(channel1, channel2, textFormatted, sb);
				formatter.Format(sb);
				textFormatted = sb.ToString();
				sb.Length = 0;

				formatter.WithUncolorizedPrefixes(channel1, channel2, textUnformatted, sb);
				textUnformatted = sb.ToString();
				sb.Length = 0;
			}
			else
            {
				textFormatted = formatter.Format(textFormatted);
            }
		}

		string methodAsString = ExpressionUtility.TargetToString(contextMethod);

		sb.Append(textFormatted);
		bool addLineBreak = !textUnformatted.EndsWith("\n");
		if(addLineBreak)
		{
			sb.Append('\n');
		}		
		sb.Append(methodAsString);
		textFormatted = sb.ToString();
		sb.Length = 0;

		sb.Append(textUnformatted);
		if(addLineBreak)
		{
			sb.Append('\n');
		}
		sb.Append(methodAsString);
		textUnformatted = sb.ToString();

		LastMessageUnformatted = textUnformatted;
		LastMessageContext = context;

		if(stackTraceIfHidden != null)
        {
			BroadcastLogMessageSuppressed(textUnformatted, textFormatted, stackTraceIfHidden, type, context);
			return;
        }

		switch(type)
		{
			case LogType.Error:
			case LogType.Exception:
				UnityEngine.Debug.LogError(textFormatted, context);
				return;
			case LogType.Assert:
				UnityEngine.Debug.Assert(false, textFormatted, context);
				return;
			case LogType.Warning:
				UnityEngine.Debug.LogWarning(textFormatted, context);
				return;
			default:
				UnityEngine.Debug.Log(textFormatted, context);
				return;
		}
		#endif
	}

	internal static bool IsTrue(Expression<Func<object>> classMember)
    {
		var value = ExpressionUtility.GetValue(classMember);
		if(ReferenceEquals(value, null))
		{
			return false;
		}
		var type = value.GetType();
		if(type == typeof(bool))
		{
			return (bool)value;
		}
		if(type == typeof(Object))
		{
			return (Object)value;
		}
		if(!typeof(IConvertible).IsAssignableFrom(type))
		{
			return true;
		}
		try
		{
			return Convert.ToBoolean(value);
		}
		catch
		{
			return true;
		}
    }

	internal static bool IsTrue(Expression<Func<bool>> classMember)
    {
		return classMember.Compile().Invoke();
    }

	internal static bool Equals([NotNull]Expression<Func<object>> classMember, [CanBeNull] object expectedValue)
	{
		var classMemberValue = ExpressionUtility.GetValue(classMember);

		if(ReferenceEquals(classMemberValue, null))
		{
			return ReferenceEquals(expectedValue, null);
		}
		if(classMemberValue.Equals(expectedValue))
		{
			return true;
		}
		if(classMemberValue.GetType() == typeof(Object))
        {
			return classMemberValue as Object == expectedValue as Object;
		}
		return false;
	}

	private static string GetFullLogFilePath([CanBeNull]string path)
	{
		if(string.IsNullOrEmpty(path))
		{
			if(string.IsNullOrEmpty(Application.consoleLogPath))
			{
				path = Application.persistentDataPath + "/LogToFile.log";
			}
			else
			{
				path = Path.Combine(Path.GetDirectoryName(Application.consoleLogPath), "LogToFile.log");
			}
			return path;
		}

		if(path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
		{
			path = Application.dataPath + path.Substring(6);
		}
		else if(path.Length >= 2 && path[1] != ':')
		{
			if(string.IsNullOrEmpty(Application.consoleLogPath))
			{
				path = Path.Combine(Application.persistentDataPath, path);
			}
			else
			{
				path = Path.Combine(Path.GetDirectoryName(Application.consoleLogPath), path);
			}
		}

		if(Path.GetExtension(path).Length == 0)
		{
			return path + ".log";
		}
		return path;
	}

	private static Object GetFirstUnityObjectOrDefault(object[] args)
    {
		for(int n = args == null ? -1 : args.Length - 1; n >= 0; n--)
		{
			var unityObject = args[n] as Object;
			if(unityObject != null)
			{
				return unityObject;
			}
		}
		return null;
    }

	/// <summary>
	/// Draws a line between specified start and end points.
	/// <para>
	/// The line will be drawn in the Game view of the editor when the game is running and the gizmo drawing is enabled. The line will also be drawn in the Scene when it is visible in the Game view. Leave the game running and showing the line. Switch to the Scene view and the line will be visible.
	/// </para>
	/// <para>
	/// The duration is the time (in seconds) for which the line will be visible after it is first displayed. A duration of zero shows the line for just one frame.
	/// </para>
	/// <para>
	/// Note: This is for debugging playmode only. Editor gizmos should be drawn with <see cref="Gizmos.DrawLine"/> or Handles.DrawLine instead.
	/// </para>
	/// <example>
	/// <code>
	/// using UnityEngine;
	/// using System.Collections;
	/// 
	/// public class ExampleClass : MonoBehaviour
	/// {
	///		public Transform target;
	///		
	///		void OnDrawGizmosSelected()
	///		{
	///			if (target != null)
	///			{
	///				// Draws a blue line from this transform to the target
	///				Gizmos.color = Color.blue;
	///				Gizmos.DrawLine(transform.position, target.position);
	///			}
	///		}
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="start"> Point in world space where the line should start. </param>
	/// <param name="end"> Point in world space where the line should end. </param>
	[Conditional("UNITY_EDITOR")] // DrawLine doesn't work in builds
	public static void DrawLine(Vector3 start, Vector3 end)
	{
		#if UNITY_EDITOR
		UnityEngine.Debug.DrawLine(start, end);
		#endif
	}

	/// <summary>
	/// Draws a line between specified start and end points.
	/// <para>
	/// The line will be drawn in the Game view of the editor when the game is running and the gizmo drawing is enabled. The line will also be drawn in the Scene when it is visible in the Game view. Leave the game running and showing the line. Switch to the Scene view and the line will be visible.
	/// </para>
	/// <para>
	/// The duration is the time (in seconds) for which the line will be visible after it is first displayed. A duration of zero shows the line for just one frame.
	/// </para>
	/// <para>
	/// Note: This is for debugging playmode only. Editor gizmos should be drawn with <see cref="Gizmos.DrawLine"/> or Handles.DrawLine instead.
	/// </para>
	/// <example>
	/// <code>
	/// using UnityEngine;
	/// using System.Collections;
	/// 
	/// public class ExampleClass : MonoBehaviour
	/// {
	///		public Transform target;
	///		
	///		void OnDrawGizmosSelected()
	///		{
	///			if (target != null)
	///			{
	///				// Draws a blue line from this transform to the target
	///				Gizmos.color = Color.blue;
	///				Gizmos.DrawLine(transform.position, target.position, Color.green);
	///			}
	///		}
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="start"> Point in world space where the line should start. </param>
	/// <param name="end"> Point in world space where the line should end. </param>
	/// <param name="color"> Color of the line. </param>
	/// <param name="duration"> How long the line should be visible for. </param>
	/// <param name="depthTest"> Should the line be obscured by objects closer to the camera? </param>
	[Conditional("UNITY_EDITOR")] // DrawLine doesn't work in builds
	public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f, bool depthTest = true)
	{
		#if UNITY_EDITOR
		UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
		#endif
	}

	/// <summary>
	/// Draws a line from start to start + dir in world coordinates.
	/// <para>
	/// The duration parameter determines how long the line will be visible after the frame it is drawn. If duration is 0 (the default) then the line is rendered 1 frame.
	/// </para>
	/// <para>
	/// If depthTest is set to true then the line will be obscured by other objects in the Scene that are nearer to the camera.
	/// </para>
	/// <para>The line will be drawn in the Scene view of the editor. If gizmo drawing is enabled in the game view, the line will also be drawn there.
	/// </para>
	/// <example>
	/// <code>
	/// using UnityEngine;
	/// 
	/// public class Example : MonoBehaviour
	/// {
	///		// Frame update example: Draws a 10 meter long green line from the position for 1 frame.
	///		void Update()
	///		{
	///			Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
	///			Debug.DrawRay(transform.position, forward);
	///		}
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="start"> Point in world space where the ray should start. </param>
	/// <param name="dir"> Direction and length of the ray. </param>
	[Conditional("UNITY_EDITOR")] // DrawRay doesn't work in builds
	public static void DrawRay(Vector3 start, Vector3 dir)
	{
		#if UNITY_EDITOR
		UnityEngine.Debug.DrawRay(start, dir);
		#endif
	}

	/// <summary>
	/// Draws a line from start to start + dir in world coordinates.
	/// <para>
	/// The duration parameter determines how long the line will be visible after the frame it is drawn. If duration is 0 (the default) then the line is rendered 1 frame.
	/// </para>
	/// <para>
	/// If depthTest is set to true then the line will be obscured by other objects in the Scene that are nearer to the camera.
	/// </para>
	/// <para>The line will be drawn in the Scene view of the editor. If gizmo drawing is enabled in the game view, the line will also be drawn there.
	/// </para>
	/// <example>
	/// <code>
	/// using UnityEngine;
	/// 
	/// public class Example : MonoBehaviour
	/// {
	///		// Frame update example: Draws a 10 meter long green line from the position for 1 frame.
	///		void Update()
	///		{
	///			Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
	///			Debug.DrawRay(transform.position, forward, Color.green);
	///		}
	/// }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="start"> Point in world space where the ray should start. </param>
	/// <param name="dir"> Direction and length of the ray. </param>
	/// <param name="color"> Color of the drawn line. </param>
	/// <param name="duration"> How long the line will be visible for (in seconds). </param>
	/// <param name="depthTest"> Should the line be obscured by other objects closer to the camera? </param>
	[Conditional("UNITY_EDITOR")] // DrawRay doesn't work in builds
	public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration = 0f, bool depthTest = true)
	{
		#if UNITY_EDITOR
		UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);
		#endif
	}

	/// <summary>
	/// Pauses the editor.
	/// <para>
	/// This is useful when you want to check certain values on the inspector and you are not able to pause it manually.
	/// </para>
	/// </summary>
	[Conditional("UNITY_EDITOR")] // Break pauses the editor and doesn't work in builds
	public static void Break()
	{
		#if UNITY_EDITOR
		UnityEngine.Debug.Break();
		#endif
	}

	/// <summary>
	/// Clears errors from the developer Console.
	/// <seealso cref="developerConsoleVisible"/>
	/// </summary>
	#if !DEBUG
	[Conditional("FALSE")]
	#endif
	public static void ClearDeveloperConsole()
	{
		#if UNITY_EDITOR
		ClearEditorConsole();
		#elif DEBUG
		UnityEngine.Debug.ClearDeveloperConsole();
		#endif
	}

	/// <summary>
	/// Clears errors from the Console window in the editor.
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	public static void ClearEditorConsole()
	{
		#if UNITY_EDITOR
		var type = Type.GetType("Sisus.Debugging.Console.ConsoleWindowPlusExperimental, Assembly-CSharp-Editor.dll");
		if(type != null)
		{
			var clearConsolePlusWindow = type.GetMethod("ClearMessages", BindingFlags.Static | BindingFlags.Public);
			if(clearConsolePlusWindow != null)
            {
				clearConsolePlusWindow.Invoke(null, null);
			}
		}

		type = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
		if(type == null)
		{
			const string msg = "Can't clear editor Console because failed to find class UnityEditor.LogEntries in UnityEditor.dll.";
			LogInternal(msg, msg, LogType.Warning, 0, 0, null, null);
			return;
		}
		var method = type.GetMethod("Clear", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
		if(method == null)
		{
			const string msg = "Can't clear editor Console because failed to find method Clear in class UnityEditor.LogEntries.";
			LogInternal(msg, msg, LogType.Warning, 0, 0, null, null);
			return;
		}
		method.Invoke(null, null);
		#endif
	}
}

#if DEBUG_LOG_EXTENSIONS_INSIDE_UNIQUE_NAMESPACE
}
#endif