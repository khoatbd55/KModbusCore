using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KUtilities.TaskExtentions
{
    public class KAsyncQueue<TItem> : IDisposable
    {
        readonly object _syncRoot = new object();
        SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();

        public int Count => _queue.Count;
        public int Limit { get; set; } = -1;
        private object lockObject = new object();

        public void Enqueue(TItem item)
        {
            lock (_syncRoot)
            {
                _queue.Enqueue(item);
                if (Limit > 0)
                {
                    lock (lockObject)
                    {
                        TItem overflow;
                        while (_queue.Count > Limit && _queue.TryDequeue(out overflow)) ;
                    }
                }
                _semaphore?.Release();
            }
        }

        public KAsyncQueue()
        {

        }

        public List<TItem> ToList()
        {
            return _queue.ToList();
        }

        public KAsyncQueue(int limit)
        {
            Limit = limit;
        }

        public async Task<KAsyncQueueDequeueResult<TItem>> TryDequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                Task task;
                lock (_syncRoot)
                {
                    if (_semaphore == null)
                    {
                        return new KAsyncQueueDequeueResult<TItem>(false, default);
                    }

                    task = _semaphore.WaitAsync(cancellationToken);
                }

                await task.ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return new KAsyncQueueDequeueResult<TItem>(false, default);
                }

                if (_queue.TryDequeue(out var item))
                {
                    return new KAsyncQueueDequeueResult<TItem>(true, item);
                }
            }
            catch (ArgumentNullException)
            {
                // The semaphore throws this internally sometimes.
                return new KAsyncQueueDequeueResult<TItem>(false, default);
            }
            catch (OperationCanceledException)
            {
                return new KAsyncQueueDequeueResult<TItem>(false, default);
            }

            return new KAsyncQueueDequeueResult<TItem>(false, default);
        }

        public KAsyncQueueDequeueResult<TItem> TryDequeue()
        {
            if (_queue.TryDequeue(out var item))
            {
                return new KAsyncQueueDequeueResult<TItem>(true, item);
            }

#pragma warning disable CS8604 // Possible null reference argument.
            return new KAsyncQueueDequeueResult<TItem>(false, default);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public void Clear()
        {
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _semaphore?.Dispose();
                _semaphore = null;
            }
        }
    }
}
