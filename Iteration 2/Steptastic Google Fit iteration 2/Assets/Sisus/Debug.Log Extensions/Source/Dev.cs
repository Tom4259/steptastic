using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
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
	/// Utility class containing Debugging methods similar to the <see cref="Debug"/> class, with the exception that
	/// all calls to its methods — including any calls made in their arguments — are completely omitted
	/// in release builds.
	/// </summary>
	public static class Dev
	{
		/// <summary>
		/// Gets the name of your personal channel which is unique to this particular computer.
		/// <para>
		/// This channel will automatically be whitelisted only on this computer.
		/// <para>
		/// Once you know the name of your personal channel you can prefix logged messages with it wrapped in brackets
		/// to have the messages only show up on your computer and not clutter up the Console for anyone else.
		/// </para>
        /// <example>
		/// <code>
		/// [MenuItem("Help/Print My Personal Channel Name")]
		/// public static void PrintMyPersonalChannel()
		/// {
		///		// This printed "JohnDoe" on my computer.
		///		Dev.Log(Dev.PersonalChannelName);
		///	}
		///	
		/// public static void LogIfJohnDoe(string message)
		/// {
		///		// Log the message using JohnDoe's personal channel.
		/// 	Dev.Log(Channel.JohnDoe, message);
		/// }
		/// </code>
		/// </example>
		/// </summary>
		public static string PersonalChannelName
		{
			get
			{
				return Environment.UserName;
			}
		}

		/// <summary>
		/// Logs a <paramref name="message"/> to the console.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void Log([CanBeNull]object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
            {
				return;
            }
			LogInternal(message, LogType.Log, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a <paramref name="message"/> to the console.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void Log([CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
            {
				return;
            }
			LogInternal(message, LogType.Log, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a <paramref name="message"/> to the console on the given <paramref name="channel"/>.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public IEnumerator PlaySound(float delay, AudioId audioId)
		/// {
		///		Dev.Log(Channel.Audio, "Playing {audioId} in {delay} seconds.", this);
		///		
		///		yield return new WaitForSeconds(delay);
		///		
		///		audioController.Play(audioId);
		///	}
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void Log(int channel, [CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			LogInternal(message, LogType.Log, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
        /// <example>
		/// <code>
		/// public IEnumerator PlaySoundEffect(float delay, AudioId audioId)
		/// {
		///		Dev.Log(Channel.Audio, Channel.Sfx, "Playing {audioId} in {delay} seconds.", this);
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
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void Log(int channel1, int channel2, [CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			LogInternal(message, LogType.Log, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public void PlaySound(AudioId audioId)
		/// {
		///		Dev.Log(Channel.Audio, audioId, this);
		///		
		///		audioController.Play(audioId);
		///	}
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void Log(int channel, [CanBeNull] object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			LogInternal(message, LogType.Log, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public void PlaySoundEffect(AudioId audioId)
		/// {
		///		Dev.Log(Channel.Audio, Channel.Sfx, audioId, this);
		///		
		///		audioController.Play(audioId);
		///	}
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel1"> First channel to which the message belongs. </param>
		/// <param name="channel2"> Second channel to which the message belongs. </param>
		/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void Log(int channel1, int channel2, [CanBeNull]object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			LogInternal(message, LogType.Log, channel1, channel2, context, !channels.IsEitherEnabled(channel1, channel2) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs to the Console the name and value of <paramref name="classMember">a class member</paramref>.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public void SetActivePage(Page value)
		/// {
		///		activePage = value;
		///		
		///		Dev.Log(()=>activePage, this);
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
		[Conditional("DEBUG")]
		public static void Log([NotNull]Expression<Func<object>> classMember, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
            {
				return;
            }
			if(context == null)
			{
				context = ExpressionUtility.GetContext(classMember);
			}
			LogInternal(classMember, LogType.Log, 0, 0, context, null);
			#endif
		}

		/// <summary>
		/// Logs a message to the Console consisting of a <paramref name="prefix">text string</paramref> followed by the names and values of
		/// <paramref name="classMembers">zero or more class members</paramref>.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public IEnumerator PlaySound(float delay, AudioId audioId)
		/// {
		///		Dev.Log("[Audio] Playing delayed - ", ()=>delay, ()=>audioId);
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
		[Conditional("DEBUG")]
		public static void Log([NotNull]string prefix, [NotNull]params Expression<Func<object>>[] classMembers)
		{
			#if DEBUG
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
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
		[Conditional("DEBUG")]
		public static void Log(params string[] messageParts)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
			LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Log, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void Log(string messagePart1, string messagePart2, params string[] messageParts)
		{
			#if DEBUG
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
		/// With shorter messages a comma will be used to separate elements in the list,
		/// and with longer message a line break will be used.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="arg1"> First listed element. </param>
		/// <param name="arg2"> Second listed element. </param>
		/// <param name="args"> (Optional) Additional listed elements. </param>
		[Conditional("DEBUG")]
		public static void Log(object arg1, object arg2, params object[] args)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			LogInternal(arg1, arg2, args, LogType.Log, 0, 0, null, ShouldHideMessage(arg1) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([CanBeNull]object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			LogInternal(message, LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			LogInternal(message, LogType.Warning, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([NotNull]string prefix, [NotNull]params Expression<Func<object>>[] classMembers)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);
			LogInternal(prefix, classMembers, LogType.Warning, 0, 0, context, null);
			#endif
		}

		/// <summary>
		/// Logs a warning <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogWarning(int channel, [CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			LogInternal(message, LogType.Warning, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a warning <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public void PlaySound(AudioId audioId)
		/// {
		///		Dev.Log(Channel.Audio, audioId, this);
		///		
		///		audioController.Play(audioId);
		///	}
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogWarning(int channel, [CanBeNull] object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			LogInternal(message, LogType.Warning, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a warning to the Console formed by joining the given text strings together.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
		[Conditional("DEBUG")]
		public static void LogWarning(params string[] messageParts)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
			LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Warning, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a warning to the Console formed by joining the given text strings together.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="messagePart1"> First part of the message. </param>
		/// <param name="messagePart2"> Second part of the message. </param>
		/// <param name="messageParts"> Additional parts of the message. </param>
		[Conditional("DEBUG")]
		public static void LogWarning(string messagePart1, string messagePart2, params string[] messageParts)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			LogInternal(formatter.JoinUncolorized(messagePart1, messagePart2, messageParts), formatter.JoinColorized(messagePart1, messagePart2, messageParts), LogType.Warning, 0, 0, null, ShouldHideMessage(messagePart1) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a warning to the Console listing a number of elements separated by a separator character.
		/// <para>
		/// With shorter messages a comma will be used to separate elements in the list,
		/// and with longer message a line break will be used.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="arg1"> First listed element. </param>
		/// <param name="arg2"> Second listed element. </param>
		/// <param name="args"> (Optional) Additional listed elements. </param>
		[Conditional("DEBUG")]
		public static void LogWarning(object arg1, object arg2, params object[] args)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			LogInternal(arg1, arg2, args, LogType.Warning, 0, 0, null, ShouldHideMessage(arg1) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogWarning([NotNull]Expression<Func<object>> classMember, [CanBeNull]Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Warning))
			{
				return;
			}
			if(context == null)
			{
				context = ExpressionUtility.GetContext(classMember);
			}
			LogInternal(classMember, LogType.Warning, 0, 0, context, null);
			#endif
		}

		/// <summary>
		/// Logs an error message to the Console consisting of the name and value of one or more class members separated by a separator character.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		/// <para>
		/// With shorter messages a comma will be used to separate elements in the list, and with longer message a line break will be used.
		/// </para>
		/// </summary>
		/// <param name="classMembers"> Expressions pointing to class members whose names and values will be included in the message. </param>
		[Conditional("DEBUG")]
		public static void LogError([NotNull]string prefix, [NotNull]params Expression<Func<object>>[] classMembers)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			var context = classMembers == null || classMembers.Length == 0 ? null : ExpressionUtility.GetContext(classMembers[0]);
			LogInternal(prefix, classMembers, LogType.Error, 0, 0, context, null);
			#endif
		}

		/// <summary>
		/// Logs an error <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogError(int channel, [CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			LogInternal(message, LogType.Error, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs an error <paramref name="message"/> to the console using the given channel.
		/// <para>
		/// Channels can be used to selectively suppress messages you don't care about at the moment.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
        /// <example>
		/// <code>
		/// public void PlaySound(AudioId audioId)
		/// {
		///		Dev.Log(Channel.Audio, audioId, this);
		///		
		///		audioController.Play(audioId);
		///	}
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogError(int channel, [CanBeNull] object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			LogInternal(message, LogType.Error, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs an error to the Console formed by joining the given text strings together.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="messageParts"> <see cref="string">strings</see> to join together to form the message. </param>
		[Conditional("DEBUG")]
		public static void LogError(params string[] messageParts)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			bool hide = messageParts != null && messageParts.Length > 0 && ShouldHideMessage(messageParts[0]);
			LogInternal(formatter.JoinUncolorized(messageParts), formatter.JoinColorized(messageParts), LogType.Error, 0, 0, null, hide ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs an error to the Console formed by joining the given text strings together.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="messagePart1"> First part of the message. </param>
		/// <param name="messagePart2"> Second part of the message. </param>
		/// <param name="messageParts"> Additional parts of the message. </param>
		[Conditional("DEBUG")]
		public static void LogError(string messagePart1, string messagePart2, params string[] messageParts)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			LogInternal(formatter.JoinUncolorized(messagePart1, messagePart2, messageParts), formatter.JoinColorized(messagePart1, messagePart2, messageParts), LogType.Error, 0, 0, null, ShouldHideMessage(messagePart1) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs an error to the Console listing a number of elements separated by a separator character.
		/// <para>
		/// With shorter messages a comma will be used for the separator character,
		/// and with longer message a line break will be used.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="arg1"> First listed element. </param>
		/// <param name="arg2"> Second listed element. </param>
		/// <param name="args"> (Optional) Additional listed elements. </param>
		[Conditional("DEBUG")]
		public static void LogError(object arg1, object arg2, params object[] args)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			LogInternal(arg1, arg2, args, LogType.Error, 0, 0, null, ShouldHideMessage(arg1) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs an error <paramref name="message"/> to the Console.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="message"> <see cref="string"/> or <see cref="object"/> to be converted to string representation for display. </param>
		/// <param name="context">
		/// <see cref="Object"/> to which the message applies.
		/// <para>
		/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
		/// </para>
		/// </param>
		[Conditional("DEBUG")]
		public static void LogError([CanBeNull]object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			LogInternal(message, LogType.Error, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs an error <paramref name="message"/> to the Console.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="message"> Message to display. </param>
		/// <param name="context">
		/// <see cref="Object"/> to which the message applies.
		/// <para>
		/// If you pass a context argument that <see cref="Object"/> will be momentarily highlighted in the Hierarchy window when you click the log message in the Console.
		/// </para>
		/// </param>
		[Conditional("DEBUG")]
		public static void LogError([CanBeNull]string message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Error))
			{
				return;
			}
			LogInternal(message, LogType.Error, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a warning message to the Console consisting of the name and value of <paramref name="classMember">a class member</paramref>.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
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
		[Conditional("DEBUG")]
		public static void LogError([NotNull]Expression<Func<object>> classMember, Object context = null)
		{
			#if DEBUG
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
		/// Logs an <paramref name="exception"/> to the Console.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="exception"> Runtime exception to display. </param>
		[Conditional("DEBUG")]
		public static void LogException(Exception exception)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Exception))
			{
				return;
			}
			LastMessageContext = null;
			UnityEngine.Debug.LogException(exception);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogAssertion(object message, Object context = null)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Assert))
			{
				return;
			}
			LogInternal(message, LogType.Assert, 0, 0, context, ShouldHideMessage(message) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogAssertionFormat(string format, params object[] args)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Assert))
			{
				return;
			}
			LogFormatInternal(format, args, LogType.Assert, 0, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		[Conditional("DEBUG")]
		public static void LogAssertionFormat(Object context, string format, params object[] args)
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Assert))
			{
				return;
			}
			LogFormatInternal(format, args, LogType.Assert, 0, 0, context, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
		}

		/// <summary>
		/// Logs a message to the Console formed by inserting the values of <paramref name="args">zero or more objects</paramref> into a <paramref name="format">text string</paramref>.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
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
		/// A format item consists of braces ("{" and "}") containing the index of the argument whose value should be inserted into the <paramref name="format"/> string at that Location.
		/// </para>
		/// </param>
		/// <param name="args">
		/// Zero or more objects to be converted to string and inserted into the <paramref name="format">composite format string</paramref>.
		/// </param>
		[Conditional("DEBUG")]
		public static void LogFormat<LogOption>(LogType logType, LogOption logOptions, Object context, string format, params object[] args) where LogOption : struct, IConvertible
		{
			#if DEBUG
			if(!IsLogTypeAllowed(LogType.Log))
			{
				return;
			}
			LogFormatInternal(format, args, LogType.Log, 0, 0, null, ShouldHideMessage(format) ? StackTraceUtility.ExtractStackTrace() : null);
			#endif
        }

		/// <summary>
		/// If <paramref name="condition"/> is <see langword="true"/> logs a <paramref name="message"/> to the console.
		/// <para>
		/// If <paramref name="condition"/> is <see langword="false"/> does nothing.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="condition"> Condition that must be <see langword="true"/> for logging to take place. </param>
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogIf(bool condition, string message, Object context = null)
        {
			#if DEBUG
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
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="condition"> Condition that must be <see langword="true"/> for logging to take place. </param>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="message"> Message to display. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogIf(int channel, bool condition, string message, Object context = null)
        {
			#if DEBUG
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
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="condition"> Condition that must be <see langword="true"/> for logging to take place. </param>
		/// <param name="channel"> The channel to which the message belongs. </param>
		/// <param name="classMember"> Expression pointing to a class member whose name and value will be logged. </param>
		/// <param name="context"> <see cref="Object"/> to which the message applies. </param>
		[Conditional("DEBUG")]
		public static void LogIf(int channel, bool condition, [NotNull] Expression<Func<object>> classMember, Object context = null)
        {
			#if DEBUG
			if(!condition || !IsLogTypeAllowed(LogType.Log))
            {
				return;
            }
			LogInternal(classMember, LogType.Log, channel, 0, context, !channels.IsEnabled(channel) ? StackTraceUtility.ExtractStackTrace() : null);
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
		/// An error will also only be logged in builds if the DEBUG symbol is defined, like for example in development builds.
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
		/// <param name="context"> <see cref="Object"/> to which the assertion applies. </param>
        /// <returns> <see langword="true"/> if <paramref name="condition"/> was <see langword="true"/>; otherwise, <see langword="false"/>. </returns>
		public static bool Ensure(bool condition, Object context = null)
		{
			if(condition)
			{
				return true;
			}
			#if DEBUG
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
		/// If <paramref name="condition"/> is <see langword="false"/> logs to the Console an error <paramref name="message"/> and returns <see langword="false"/>.
		/// <para>
		/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
		/// </para>
		/// <para>
		/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
		/// </para>
		/// <para>
		/// An error will also only be logged in builds if the DEBUG symbol is defined, like for example in development builds.
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
		public static bool Ensure(bool condition, string message, Object context = null)
		{
			if(condition)
			{
				return true;
			}

			#if DEBUG
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
		/// an error message and returns <see langword="false"/>.
		/// <para>
		/// If condition is <see langword="true"/> returns <see langword="true"/> without logging anything.
		/// </para>
		/// <para>
		/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
		/// </para>
		/// <para>
		/// An error will also only be logged in builds if the DEBUG symbol is defined, like for example in development builds.
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

			#if DEBUG
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
		/// An error will only be logged in builds if the DEBUG symbol is defined, like for example in development builds.
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
		public static bool Guard(bool condition, Object context = null)
		{
			if(condition)
			{
				return false;
			}

			#if DEBUG
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
		/// <para>
		/// This can be useful for checking that the arguments passed to a function are valid and if not returning early with an error.
		/// </para>
		/// <para>
		/// An error is only logged the first time during a session that the <paramref name="condition"/> evaluates to <see langword="false"/> to avoid flooding the log file.
		/// </para>
		/// <para>
		/// An error will only be logged in builds if the DEBUG symbol is defined, like for example in development builds.
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
		public static bool Guard(int channel, bool condition, Object context = null)
		{
			if(condition)
			{
				return false;
			}

			#if DEBUG
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
		/// Calls to this method will be fully stripped from release builds. If you don't want this behaviour you can use
		/// <see cref="Debug.Guard{TException}(bool, object[])"/> or <see cref="Critical.Guard{TException}(bool, object[])"/> instead.
		/// </para>
        /// <example>
		/// <code>
		/// private void CopyComponent(Component component, GameObject to)
		/// {
		///		// These guard clauses will only exist in the editor and in development builds.
		///		Dev.Guard<ArgumentNullException>(component != null, nameof(component)));
		///		Dev.Guard<ArgumentNullException>(to != null, nameof(to));
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
		[Conditional("DEBUG")]
		public static void Guard<TException>(bool condition, params object[] exceptionArguments) where TException : Exception, new()
		{
			#if DEBUG
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
		/// Logs to the Console the name and value of every field and property of <paramref name="target"/> matched using the specified <paramref name="flags"/>.
		/// <para>
		/// With a small number of listed members a comma will be used to separate them, and with a larger number of members a line break will be used.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="target"> <see cref="object"/> instance whose class members are to be listed. </param>
		/// <param name="flags">
		/// <see cref="BindingFlags"/> used when searching for the members.
		/// <para>
		/// By default only public and non-inherited instance members are listed.
		/// </para>
		/// </param>
        [Conditional("DEBUG")]
		public static void LogState([CanBeNull]object target, BindingFlags flags = DefaultInstanceBindingFlags)
		{
			#if DEBUG
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
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		/// Logs to the Console the name and value of <paramref name="classMember"/> any time its value is changed.
		/// <para>
		/// This will continue happening until <see cref="CancelLogChanges(MemberInfo)"/> is called with an
		/// expression pointing to the same class member.
		/// </para>
		/// <para>
		/// At runtime logging takes place at the end of each frame.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		/// <param name="classMember"> Expression pointing to the class member to track. </param>
		/// <param name="pauseOnChanged"> If <see langword="true"/>
		/// then the editor will be paused whenever the value of the class member changes; otherwise, editor will not be paused.
		/// <para>
		/// In builds this parameter will have no effect; the application will not be paused regardless of its value.
		/// </para>
		/// </param>
		[Conditional("DEBUG")]
		public static void LogChanges(Expression<Func<object>> classMember, bool pauseOnChanged = false)
		{
			#if DEBUG
			Debug.LogChanges(classMember, pauseOnChanged);
			#endif
		}

		[Conditional("DEBUG")]
		public static void CancelLogChanges(Expression<Func<object>> classMember)
		{
			#if DEBUG
			Debug.CancelLogChanges(classMember);
			#endif
		}

		[Conditional("DEBUG")]
		public static void DisplayOnScreen([NotNull]Expression<Func<object>> classMember)
		{
			#if DEBUG
			Debug.DisplayOnScreen(classMember);
			#endif
		}

		[Conditional("DEBUG")]
		public static void CancelDisplayOnScreen([NotNull]Expression<Func<object>> classMember)
		{
			#if DEBUG
			Debug.CancelDisplayOnScreen(classMember);
			#endif
		}

		/// <summary>
		/// Start displaying a button on screen which calls a method when clicked.
		/// <para>
		/// Button will continue to be displayed until <see cref="CancelDisplayButton(Expression{Action})"/>
		/// is called with an expression pointing to the same method.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		[Conditional("DEBUG")]
		public static void DisplayButton([NotNull]Expression<Action> onClicked)
		{
			#if DEBUG
			Debug.DisplayButton(onClicked);
			#endif
		}

		/// <summary>
		/// Start displaying a button on screen which calls a method when clicked.
		/// <para>
		/// Button will continue to be displayed until <see cref="CancelDisplayButton(string)"/>
		/// is called with the same label.
		/// </para>
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		[Conditional("DEBUG")]
		public static void DisplayButton([NotNull]string label, [NotNull]Action onClicked)
		{
			#if DEBUG
			Debug.DisplayButton(label, onClicked);
			#endif
		}

		/// <summary>
		/// Stop displaying a button on screen.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		/// Expression pointing to a method that is being displayed on screen as a button.
		/// </param>
		[Conditional("DEBUG")]
		public static void CancelDisplayButton([NotNull]Expression<Action> onClicked)
		{
			#if DEBUG
			Debug.CancelDisplayButton(onClicked);
			#endif
		}

		/// <summary>
		/// Stop displaying a button on screen.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
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
		/// <param name="label"> Label of a button that is being displayed on screen. </param>
		[Conditional("DEBUG")]
		public static void CancelDisplayButton([NotNull]string label)
		{
			#if DEBUG
			Debug.CancelDisplayButton(label);
			#endif
		}

		/// <summary>
		/// Starts a new stopwatch counting upwards from zero with the given label.
		/// <para>
		/// Calls to this method will be fully stripped from release builds.
		/// </para>
		/// </summary>
		[Conditional("DEBUG")]
		public static void StartStopwatch()
		{
			#if DEBUG
			Debug.StartStopwatch();
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
		[Conditional("DEBUG")]
		public static void StartStopwatch([NotNull]string name)
		{
			#if DEBUG
			Debug.StartStopwatch(name);
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
		[Conditional("DEBUG")]
		public static void StartSubStopwatch([NotNull]string name)
		{
			#if DEBUG
			Debug.StartSubStopwatch(name);
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
		[Conditional("DEBUG")]
		public static void StartSubStopwatch([NotNull]string parentName, [NotNull]string name)
		{
			#if DEBUG
			Debug.StartSubStopwatch(name, name);
			#endif
		}

		/// <summary>
		/// Gets the last started stopwatch and finishes the sub-stopwatch inside it which was last started,
		/// still leaving the main stopwatch running.
		/// <para>
		/// Results are not logged at this point, only when you finish the main stopwatch.
		/// </para>
		/// </summary>
		[Conditional("DEBUG")]
		public static void FinishSubStopwatch()
		{
			#if DEBUG
			Debug.FinishSubStopwatch();
			#endif
		}

		/// <summary>
		/// Finishes a previously created sub-stopwatch, still leaving the main stopwatch running.
		/// Results are not logged at this point, only when you finish the main stopwatch.
		/// </summary>
		/// <param name="mainStopwatchName"> Name of main stopwatch. </param>
		[Conditional("DEBUG")]
		public static void FinishSubStopwatch([NotNull]string mainStopwatchName)
		{
			#if DEBUG
			Debug.FinishSubStopwatch(mainStopwatchName);
			#endif
		}

		/// Logs results of a previously created stopwatch and then clears it.
		[Conditional("DEBUG")]
		public static void FinishStopwatch()
		{
			#if DEBUG
			Debug.FinishStopwatch();
			#endif
		}

		/// Logs results of a previously created stopwatch and then clears it.
		[Conditional("DEBUG")]
		public static void FinishStopwatch([NotNull]string stopwatchName)
		{
			#if DEBUG
			Debug.FinishStopwatch(stopwatchName);
			#endif
		}

		/// Logs results of a all previously created stopwatches and the clears them.
		[Conditional("DEBUG")]
		public static void FinishAllStopwatches()
		{
			#if DEBUG
			Debug.FinishAllStopwatches();
			#endif
		}
    }
}