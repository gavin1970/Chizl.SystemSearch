using System;
using System.Threading;

namespace Chizl.SystemSearch
{
    internal static class SearchMessage
    {
        //private static int _cleanedUp = 0;

        static SearchMessage()
        {
#if NETCOREAPP || NET5_0_OR_GREATER
            var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(typeof(SearchMessage).Assembly);
            context.Unloading += OnUnloading;
#endif
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

#if NETCOREAPP || NET5_0_OR_GREATER
        private static void OnUnloading(System.Runtime.Loader.AssemblyLoadContext context)
        {
            //Cleanup();
        }
#endif
        private static void OnProcessExit(object sender, EventArgs e)
        {
            //Cleanup();
        }

        public static event SearchEventHandler EventMessaging;
        public static void SendMsg(SearchMessageType msgType, string msg) => EventMessaging?.Invoke(typeof(SearchMessage), new SearchEventArgs(msgType, msg));
        public static void SendMsg(string msg) => SendMsg(SearchMessageType.Info, msg);
        public static void SendMsg(Exception ex, string msg = null)
        {
            if (ex != null && !string.IsNullOrWhiteSpace(ex.Message))
            {
                var newMsg = "";
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    var checkMsg = msg.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim();
                    while (msg.EndsWith("\r") || msg.EndsWith("\n"))
                        msg = msg.Substring(0, msg.Length - 1);

                    newMsg += string.IsNullOrWhiteSpace(checkMsg) ? "" : $"{msg.Trim()}\n";
                }
                newMsg += ex.Message;

                SendMsg(SearchMessageType.Exception, newMsg);
            }
        }
    }
}
