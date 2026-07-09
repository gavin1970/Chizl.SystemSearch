using System;
using System.Collections.Generic;

namespace Chizl.SystemSearch
{
    [Flags]
    public enum LookupStatus
    {
        /// <summary>
        /// Initial setup
        /// </summary>
        NotStarted = 1,
        /// <summary>
        /// Also know as InProgress, but have extensions and IsRunning looks better than IsInProgress.
        /// </summary>
        Running = 2,
        Completed = 4,
        /// <summary>
        /// Dec: 6 - Used for starter thread, if already running or fullscan already complete, no need to do it again.
        /// </summary>
        RunningOrCompleted = Running | Completed,            // 6        
        Aborted = 8,
        NotFinished = NotStarted | Running | Aborted,        // 11
        Ended = Completed | Aborted,                         // 12
        Stopped = NotStarted | Ended,                        // 13
    }

    internal enum SleepType
    {
        Milliseconds,
        Seconds,
        Minutes,
        Hours
    }
    internal enum ReturnCase
    {
        AsIs = 0,
        Upper = 1,
        Lower = 2
    }

    internal static class SearchExt
    {
        /// <summary>
        ///     Ended = Completed | Aborted - Dec: 12
        /// </summary>
        /// <returns>True if status is Completed or Aborted</returns>
        public static bool HasEnded(this LookupStatus @this) => (@this & LookupStatus.Ended) > 0;
        /// <summary>
        ///     Completed = 4
        /// </summary>
        /// <returns>True if status is Completed</returns>
        public static bool HasCompleted(this LookupStatus @this) => (@this & LookupStatus.Completed) > 0;
        /// <summary>
        ///     Aborted = 8
        /// </summary>
        /// <returns>True if status is IsAborted</returns>
        public static bool HasAborted(this LookupStatus @this) => (@this & LookupStatus.Aborted) > 0;
        /// <summary>
        ///     Running = 2
        /// </summary>
        /// <returns>True if status is Running</returns>
        public static bool IsRunning(this LookupStatus @this) => (@this & LookupStatus.Running) > 0;
        /// <summary>
        ///     NotStarted = 1
        /// </summary>
        /// <returns>True if status is NotStarted</returns>
        public static bool HasNotStarted(this LookupStatus @this) => (@this & LookupStatus.NotStarted) > 0;
        /// <summary>
        ///     NotFinished = NotStarted | InProgress | Aborted - Dec: 11
        /// </summary>
        /// <returns>True if status is NotStarted or InProgress or Aborted</returns>
        public static bool HasNotFinished(this LookupStatus @this) => (@this & LookupStatus.NotFinished) > 0;
        /// <summary>
        ///     RunningOrCompleted = Running | Completed - Dec: 6
        /// </summary>
        /// <returns>True if status is Running or Completed</returns>
        public static bool IsRunningOrCompleted(this LookupStatus @this) => (@this & LookupStatus.RunningOrCompleted) > 0;
        /// <summary>
        ///     Stopped = NotStarted | Finished - 13
        /// </summary>
        /// <returns>True if status is NotStarted | Finished</returns>
        public static bool HasStopped(this LookupStatus @this) => (@this & LookupStatus.Stopped) > 0;
        public static string[] SplitByStr(this string @this, string splitString)
        {
            List<string> retVal = new List<string>();
            var str = @this;
            int s = 0;
            int e = str.IndexOf(splitString);

            if (e > -1)
            {
                while (e > -1)
                {
                    retVal.Add(str.Substring(s, e));
                    s = e + splitString.Length;
                    e = str.IndexOf(splitString, s);
                }

                retVal.Add(str.Substring(s, str.Length - s));
            }
            else
                retVal.Add(@this);

            return retVal.ToArray();
        }
    }

    public static class PublicExt
    {
        internal static readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalSpan"></param>
        /// <returns></returns>
        public static string FormatAsTimer(this TimeSpan totalSpan)
        {
            var retVal = new List<string>();
            if (totalSpan.TotalDays >= 1)
                retVal.Add($"{totalSpan.Days:0}d");
            if (totalSpan.TotalHours >= 1)
                retVal.Add($"{totalSpan.Hours:0}h");
            if (totalSpan.TotalMinutes >= 1)
                retVal.Add($"{totalSpan.Minutes:0}m");
            if (totalSpan.TotalSeconds >= 1)
                retVal.Add($"{totalSpan.Seconds:00}s");

            if (retVal.Count == 0)
                retVal.Add($"{totalSpan.Milliseconds:0000}ms");

            return string.Join(" ", retVal.ToArray());
        }
        public static string FormatByComma(this int inSize) => inSize.ToString("N0");
        public static string FormatByComma(this long inSize) => inSize.ToString("N0");
        public static string FormatByteSize(this int intBytes) => ((double)intBytes).FormatByteSize();
        public static string FormatByteSize(this long intBytes) => ((double)intBytes).FormatByteSize();
        public static string FormatByteSize(this double dblBytes)
        {
            int idx = 0;
            double num = dblBytes;

            while (num > 1024)
            {
                num /= 1024;
                idx++;
            }
            return string.Format("{0:n2} {1}", num, suffixes[idx]);
        }
    }
}
