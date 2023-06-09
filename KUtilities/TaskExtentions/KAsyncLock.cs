using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KUtilities.TaskExtentions
{
    public sealed class KAsyncLock
    {
        readonly object _syncRoot = new object();
        readonly Task<IDisposable> _releaser;

        SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public KAsyncLock()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            Task task;

            // This lock is required to avoid ObjectDisposedExceptions.
            // These are fired when this lock gets disposed (and thus the semaphore)
            // and a worker thread tries to call this method at the same time.
            // Another way would be catching all ObjectDisposedExceptions but this situation happens
            // quite often when clients are disconnecting.
            lock (_syncRoot)
            {
                task = _semaphore?.WaitAsync(cancellationToken);
            }

            if (task == null)
            {
                throw new ObjectDisposedException("The AsyncLock is disposed.");
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                return _releaser;
            }

            // Wait for the _WaitAsync_ method and return the releaser afterwards.
            return task.ContinueWith(
                (_, state) => (IDisposable)state,
                _releaser.Result,
                cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _semaphore?.Dispose();
                _semaphore = null;
            }
        }

        internal void Release()
        {
            lock (_syncRoot)
            {
                _semaphore?.Release();
            }
        }

        sealed class Releaser : IDisposable
        {
            readonly KAsyncLock _lock;

            internal Releaser(KAsyncLock @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                _lock.Release();
            }
        }
    }
}
