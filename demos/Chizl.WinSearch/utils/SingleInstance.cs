using System.Threading;

namespace Chizl.Applications
{
    public static class SingleInstance
    {
        private static Mutex mutex;

        public static bool IsRunning(string appName)
        {
            mutex = new Mutex(initiallyOwned: true, appName, out var createdNew);
            return !createdNew;
        }
    }

}
