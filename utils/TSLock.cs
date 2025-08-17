//using System;
//using System.Collections.Concurrent;
//using System.Linq;
//using System.Threading;

//namespace Chizl.ThreadSupport
//{
//    internal class LockTime
//    {
//        public long Id { get; set; }
//        public Guid LockObject { get; set; }
//        public DateTime ExpireDate { get; set; }
//    }
//    public static class TSLock
//    {
//        static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(20);
//        // Using long instead of int, because netstandard2.0
//        // and Interlocked.Read(), doesn't have a "ref int" option.
//        private static long _boolValue;
//        private static readonly ConcurrentDictionary<Guid, LockTime> _lockDict = new ConcurrentDictionary<Guid, LockTime>();
//        private static readonly ConcurrentDictionary<long, LockTime> _queueDict = new ConcurrentDictionary<long, LockTime>();
//        const int EVENT_SHUTDOWN = 0;
//        const int EVENT_DROPPED = 0;
//        //private static readonly ConcurrentBag<LockTime> _guidBag = new ConcurrentBag<LockTime>();
//        private static readonly AutoResetEvent[] _autoResetEvents = new AutoResetEvent[2] { 
//                                                                      new AutoResetEvent(false),    //EVENT_SHUTDOWN
//                                                                      new AutoResetEvent(false) };  //EVENT_DROPPED

//        static TSLock() {}

//        private static long _id = 0;
//        public static bool Lock(Guid lockObject, TimeSpan timeout)
//        {
//            var now = DateTime.UtcNow;

//            var lockTime = new LockTime()
//            {
//                Id = Interlocked.Increment(ref _id),
//                LockObject = lockObject,
//                ExpireDate = now.Add(timeout)
//            };

//            if (_lockDict.TryAdd(lockObject, lockTime))
//                return true;
//            else if (_lockDict.TryGetValue(lockObject, out var outLockTime) && outLockTime.ExpireDate <= now)
//            {
//                //could of been removed between start of this method, till now, so lets try to add.
//                if (!_lockDict.TryUpdate(lockObject, lockTime, outLockTime))
//                    throw new Exception($"Dictionary will not allow update or add for existing guid '{lockObject}'.");
//            }

//            _queueDict.TryAdd(lockTime.Id, lockTime);

//            while (true)
//            {
//                var evnt = WaitHandle.WaitAny(_autoResetEvents, timeout);
//                if (evnt == WaitHandle.WaitTimeout || DateTime.UtcNow <= lockTime.ExpireDate)
//                {
//                    _lockDict.TryRemove(lockObject, out _);
//                    _queueDict.TryRemove(lockTime.Id, out _);
//                    return false;
//                }
//                else 
//                {
//                    var findLockTime = _lockDict.OrderBy(o => o.Value.Id).FirstOrDefault(w => w.Key.Equals(lockObject));
//                    if (findLockTime.Value.Id.Equals(lockTime.Id))
//                    {
//                        _lockDict.TryRemove(lockObject, out _);
//                        _queueDict.TryRemove(lockTime.Id, out _);
//                        return true;
//                    }
//                }
//            }

//            return false;
//        }

//        public static bool Lock(Guid lockObject) => Lock(lockObject, _defaultTimeout);
//    }
//}
