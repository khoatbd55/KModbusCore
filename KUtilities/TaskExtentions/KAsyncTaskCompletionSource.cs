﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.TaskExtentions
{
    public sealed class KAsyncTaskCompletionSource<TResult>
    {
        readonly TaskCompletionSource<TResult> _taskCompletionSource;

        public KAsyncTaskCompletionSource()
        {
#if NET452
            _taskCompletionSource = new TaskCompletionSource<TResult>();
#else
            _taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }

        public Task<TResult> Task => _taskCompletionSource.Task;

        public bool TrySetCanceled()
        {
#if NET452
            // To prevent deadlocks it is required to call the _TrySetCanceled_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            // NET452 does not support RunContinuationsAsynchronously
            System.Threading.Tasks.Task.Run(() => _taskCompletionSource.TrySetCanceled());
            return true;
#else
            return _taskCompletionSource.TrySetCanceled();
#endif
        }

        public bool TrySetException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

#if NET452
            // To prevent deadlocks it is required to call the _TrySetException_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            // NET452 does not support RunContinuationsAsynchronously
            System.Threading.Tasks.Task.Run(() => _taskCompletionSource.TrySetException(exception));
            return true;
#else
            return _taskCompletionSource.TrySetException(exception);
#endif
        }

        public bool TrySetResult(TResult result)
        {
#if NET452
            // To prevent deadlocks it is required to call the _TrySetResult_ method
            // from a new thread because the awaiting code will not(!) be executed in
            // a new thread automatically (due to await). Furthermore _this_ thread will
            // do it. But _this_ thread is also reading incoming packets -> deadlock.
            // NET452 does not support RunContinuationsAsynchronously
            System.Threading.Tasks.Task.Run(() => _taskCompletionSource.TrySetResult(result));
            return true;
#else
            return _taskCompletionSource.TrySetResult(result);
#endif
        }
    }
}
