using System.Threading;

namespace Chizl.SystemSearch
{
    internal static class AutoEvent
    {
        public static int Shutdown => 0;
        public static int FileInfoQueue = 1;
    }

    internal static class Internals
    {
        //private static OptionalScanPaths OptionalScanPath => ScanPaths;
        public static readonly AutoResetEvent[] AutoEvents = new AutoResetEvent[]
        {
            new AutoResetEvent(false),  //AutoEvent.Shutdown
            new AutoResetEvent(false)   //AutoEvent.FileInfoQueue
        };
    }
}
