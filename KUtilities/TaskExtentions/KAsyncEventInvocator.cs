using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.TaskExtentions
{
    public readonly struct KAsyncEventInvocator<TEventArgs>
    {
        readonly Action<TEventArgs> _handler;
        readonly Func<TEventArgs, Task> _asyncHandler;

        public KAsyncEventInvocator(Action<TEventArgs> handler, Func<TEventArgs, Task> asyncHandler)
        {
            _handler = handler;
            _asyncHandler = asyncHandler;
        }

        public bool WrapsHandler(Action<TEventArgs> handler)
        {
            return ReferenceEquals(_handler, handler);
        }

        public bool WrapsHandler(Func<TEventArgs, Task> handler)
        {
            return ReferenceEquals(_asyncHandler, handler);
        }

        public Task InvokeAsync(TEventArgs eventArgs)
        {
            if (_handler != null)
            {
                _handler.Invoke(eventArgs);
                return Task.CompletedTask;
            }

            if (_asyncHandler != null)
            {
                return _asyncHandler.Invoke(eventArgs);
            }

            throw new InvalidOperationException();
        }
    }
}
