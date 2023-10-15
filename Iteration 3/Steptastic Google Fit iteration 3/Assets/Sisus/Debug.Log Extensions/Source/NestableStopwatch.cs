using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Debugging
{
	internal class NestableStopwatch
	{
		private const MethodImplOptions AggressiveInlining = (MethodImplOptions)256; // MethodImplOptions.AggressiveInlining only exists in .NET 4.5. and later

		public readonly string name;
		private readonly Stopwatch stopwatch = new Stopwatch();
		private readonly List<NestableStopwatch> runningSubStopwatches = new List<NestableStopwatch>();
		private readonly List<NestableStopwatch> finishedSubStopwatches = new List<NestableStopwatch>();

		public NestableStopwatch([NotNull]string stopwatchName)
		{
			name = stopwatchName;
			stopwatch.Start();
		}

		public void StartSubStopwatch()
		{
			StartSubStopwatch("Sub Stopwatch " + (runningSubStopwatches.Count + 1));
		}

		public void StartSubStopwatch([NotNull]string subStopwatchName)
		{
			int count = runningSubStopwatches.Count;
			if(count == 0)
			{
				runningSubStopwatches.Add(new NestableStopwatch(subStopwatchName));
			}
			else
			{
				runningSubStopwatches[count - 1].StartSubStopwatch(subStopwatchName);
			}
		}

		public void FinishSubStopwatch()
		{
			int count = runningSubStopwatches.Count;
			if(count == 0)
			{
				Debug.LogWarning("FinishSubStopwatch was called but there were no sub-stopwatches running. Make sure that the number of StartSubStopwatch and FinishSubStopwatch calls are always equal.");
				return;
			}

			var subStopwatch = runningSubStopwatches[count - 1];
			subStopwatch.FinishLastStopwatchInChildren(this);
		}

		public void FinishSubStopwatch([NotNull]string subStopwatchName)
		{
			int count = runningSubStopwatches.Count;
			for(int n = count - 1; n >= 0; n--)
			{
				var subStopwatch = runningSubStopwatches[n];
				if(string.Equals(subStopwatch.name, subStopwatchName))
				{
					subStopwatch.FinishLastStopwatchInChildren(this);
					return;
				}
			}

			Debug.LogWarning("FinishSubStopwatch(\""+ subStopwatchName + "\") was called but there were no sub-stopwatches with such name.");
		}

		public void FinishAndLogResults(DebugFormatter formatter)
		{
			Finish();
			LogResults(formatter);
		}

		#if UNITY_EDITOR
		[MethodImpl(AggressiveInlining)]
		#endif
		public void Finish()
		{
			for(int n = runningSubStopwatches.Count - 1; n >= 0; n--)
			{
				var subStopwatch = runningSubStopwatches[n];
				subStopwatch.Finish();
				runningSubStopwatches.RemoveAt(n);
				finishedSubStopwatches.Add(subStopwatch);
			}

			stopwatch.Stop();
		}

		#if UNITY_EDITOR
		[MethodImpl(AggressiveInlining)]
		#endif
		public void LogResults(DebugFormatter formatter)
		{
			#if DEV_MODE
			Debug.Assert(runningSubStopwatches.Count == 0);
			#endif

			var sb = new StringBuilder((name.Length + 20) * (finishedSubStopwatches.Count + 1));
			GetResults(formatter, sb, 0);

			#if DEV_MODE
			Debug.Assert(sb.Length > 0);
			#endif

			UnityEngine.Debug.Log(sb.ToString());
		}

		public bool TryFindStopwatchInChildren(string name, out NestableStopwatch result)
        {
			int count = runningSubStopwatches.Count;
			for(int i = count - 1; i >= 0; i--)
			{
				NestableStopwatch stopwatch = runningSubStopwatches[i];
				if(string.Equals(stopwatch.name, name))
                {
					result = stopwatch;
					return true;
				}
				if(stopwatch.TryFindStopwatchInChildren(name, out result))
                {
					return true;
                }
			}

			for(int i = count - 1; i >= 0; i--)
			{
				NestableStopwatch stopwatch = runningSubStopwatches[i];
				if(stopwatch.TryFindStopwatchInChildren(name, out result))
                {
					return true;
                }
			}

			result = null;
			return false;
        }


		#if UNITY_EDITOR
		[MethodImpl(AggressiveInlining)]
		#endif
		private void FinishLastStopwatchInChildren([NotNull]NestableStopwatch parent)
		{
			NestableStopwatch child = this;
			int count = child.runningSubStopwatches.Count;
			while(count != 0)
            {
				parent = child;
				child = child.runningSubStopwatches[count - 1];
				count = child.runningSubStopwatches.Count;
			}

			#if DEV_MODE
			Debug.Assert(child.stopwatch.IsRunning);
			#endif

			parent.runningSubStopwatches.RemoveAt(parent.runningSubStopwatches.Count - 1);
			parent.finishedSubStopwatches.Add(this);
			child.stopwatch.Stop();
		}

		private void GetResults(DebugFormatter formatter, StringBuilder sb, int indentation)
		{
			sb.Append(GetLabelAndElapsedTime(formatter, sb, indentation));

			indentation++;
			for(int n = 0, count = finishedSubStopwatches.Count; n < count; n++)
			{
				sb.Append("\n");
				finishedSubStopwatches[n].GetResults(formatter, sb, indentation);
			}
		}

		[NotNull]
		private string GetLabelAndElapsedTime(DebugFormatter formatter, StringBuilder sb, int indentation)
		{
			for(int n = indentation - 1; n >= 0; n--)
			{
				sb.Append("   ");
			}

			sb.Append(name);
			sb.Append(" . . . ");

			int timeInteger = Mathf.RoundToInt(stopwatch.ElapsedMilliseconds * 10L);
			string timeInSeconds = (timeInteger / 10000f).ToString(CultureInfo.CurrentCulture);
			if(formatter.colorize)
			{
				sb.Append(formatter.BeginNumeric);
				sb.Append(timeInSeconds);
				sb.Append("</color> s");
			}
			else
			{
				sb.Append(timeInSeconds);
				sb.Append(" s");
			}

			string result = sb.ToString();
			sb.Length = 0;
			return result;
		}
	}
}