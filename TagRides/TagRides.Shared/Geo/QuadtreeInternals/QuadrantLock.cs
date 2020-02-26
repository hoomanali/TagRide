using System;
using System.Threading;

namespace TagRides.Shared.Geo
{
    public partial class ConcurrentGeoQuadtree<T>
    {
        class QuadrantLock
        {
            public enum LockType
            {
                Shared, Upgradeable, Exclusive
            }

            public bool ThreadOwnsUpgradeableLock => rwLock.IsUpgradeableReadLockHeld;
            public bool ThreadOwnsSharedLock => rwLock.IsReadLockHeld;
            public bool ThreadOwnsExlusiveLock => rwLock.IsWriteLockHeld;

            public void EnterLock(LockType type)
            {
                switch (type)
                {
                    case LockType.Shared:
                        EnterSharedLock();
                        break;
                    case LockType.Upgradeable:
                        EnterUpgradeableLock();
                        break;
                    case LockType.Exclusive:
                        EnterExclusiveLock();
                        break;
                }
            }

            public void ExitLock(LockType type)
            {
                switch (type)
                {
                    case LockType.Shared:
                        ExitSharedLock();
                        break;
                    case LockType.Upgradeable:
                        ExitUpgradeableLock();
                        break;
                    case LockType.Exclusive:
                        ExitExclusiveLock();
                        break;
                }
            }

            public void EnterSharedLock()
            {
                rwLock.EnterReadLock();
            }

            public void ExitSharedLock()
            {
                rwLock.ExitReadLock();
            }

            public void EnterExclusiveLock()
            {
                rwLock.EnterWriteLock();
            }

            public void ExitExclusiveLock()
            {
                rwLock.ExitWriteLock();
            }

            public void EnterUpgradeableLock()
            {
                rwLock.EnterUpgradeableReadLock();
            }

            public void ExitUpgradeableLock()
            {
                rwLock.ExitUpgradeableReadLock();
            }

            // TODO: Each of these uses 5KB of memory, which is insane.
            ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
        }
    }
}
