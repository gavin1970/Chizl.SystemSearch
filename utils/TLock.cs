using System;
using System.Collections.Concurrent;

namespace Chizl.ThreadSupport
{
    public sealed class TLock : IDisposable
    {
        private bool disposedValue;
        private static bool _shutdown = false;
        private static ConcurrentDictionary<Guid, DateTime> _locks = new ConcurrentDictionary<Guid, DateTime>();

        private TLock() { }
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _shutdown = true;
                    _locks.Clear();
                }

                disposedValue = true;
            }
        }
        ~TLock() => Dispose(disposing: false);
        void IDisposable.Dispose() => Destroy();
        public void Destroy()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public static bool SetLock(Guid guidLookup, TimeSpan maxLockTime)
        {
            var timeout = DateTime.UtcNow.Add(maxLockTime);
            // try lock, if not, wait for release.
            while (!_locks.TryAdd(guidLookup, timeout))
            {
                //if timedout, return no lock available.
                if (timeout < DateTime.UtcNow || _shutdown)
                    return false;
            }

            // lock is available, remove from dictionary.
            return _locks.TryRemove(guidLookup, out timeout);
        }
    }
}
