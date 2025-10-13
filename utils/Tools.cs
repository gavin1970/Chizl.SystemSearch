using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chizl.SystemSearch
{
    internal static class Tools
    {
        /// <summary>
        /// Can use Sleep() for methods or tasks <b>not</b> using "async"<br/>
        /// Second argument: sleepType is optional with Default as Seconds.
        /// <code>
        /// Example:
        ///     Sleep(2);                           // (Default) 2 Seconds.
        ///     Sleep(2, SleepType.Milliseconds);   // 2 Milliseconds.
        /// </code>
        /// </summary>
        /// <param name="timing">How long to wait based on sleepType</param>
        /// <param name="sleepType">(Optional) Default: Seconds</param>
        internal static void Sleep(int timing, SleepType sleepType = SleepType.Seconds) => Delay(timing, sleepType).Wait();
        /// <summary>
        /// Must use Delay() for methods or tasks using "async".<br/>
        /// Second argument: sleepType is optional with Default as Seconds.
        /// <code>
        /// Example:
        ///     await Delay(2)                          // (Default) 2 Seconds.
        ///     await Delay(2, SleepType.Milliseconds)  // 2 Milliseconds.
        /// </code>
        /// </summary>
        /// <param name="timing">How long to wait based on sleepType</param>
        /// <param name="sleepType">(Optional) Default: Seconds</param>
        /// <returns></returns>
        internal static async Task Delay(int timing, SleepType sleepType = SleepType.Seconds)
        {
            TimeSpan ts = TimeSpan.Zero;
            switch (sleepType)
            {
                case SleepType.Milliseconds:
                    ts = TimeSpan.FromMilliseconds(timing);
                    break;
                case SleepType.Seconds:
                    ts = TimeSpan.FromSeconds(timing);
                    break;
                case SleepType.Minutes:
                    ts = TimeSpan.FromMinutes(timing);
                    break;
                case SleepType.Hours:
                    ts = TimeSpan.FromHours(timing);
                    break;
            }

            await Task.Delay(ts);
        }
        /// <summary>
        /// Create a 32-byte MD5 Hash without spaces.
        /// </summary>
        /// <param name="input">String to create MD5 hash.</param>
        /// <returns>MD5 Hash without dashes</returns>
        internal static string CreateMD5(string input, ReturnCase caseType = ReturnCase.Upper)
        {
            // Use input string to calculate MD5 hash
            using (MD5 md5 = MD5.Create())
                return BytesToString(md5.ComputeHash(Encoding.ASCII.GetBytes(input)), caseType);
        }
        private static string BytesToString(byte[] input, ReturnCase caseType)
        {
            var hashReturn = BitConverter.ToString(input).Replace("-", "");
            return caseType.Equals(ReturnCase.Upper) ? hashReturn.ToUpper() : hashReturn.ToLower();
        }
        public static string[] SplitOn(this string @this, char[] sep) => @this.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        public static string[] SplitOn(this string @this, char sep) => @this.Split(new char[] { sep }, StringSplitOptions.RemoveEmptyEntries);
    }
}
