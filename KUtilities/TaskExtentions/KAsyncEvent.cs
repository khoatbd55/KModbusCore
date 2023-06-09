using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.TaskExtentions
{
    public class KAsyncEvent<TEventArgs> where TEventArgs : EventArgs
    {
        readonly List<KAsyncEventInvocator<TEventArgs>> _handlers = new List<KAsyncEventInvocator<TEventArgs>>();

        public bool HasHandlers => _handlers.Count > 0;

        public void AddHandler(Func<TEventArgs, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.Add(new KAsyncEventInvocator<TEventArgs>(null, handler));
        }

        public void AddHandler(Action<TEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.Add(new KAsyncEventInvocator<TEventArgs>(handler, null));
        }

        public async Task InvokeAsync(TEventArgs eventArgs)
        {
            foreach (var handler in _handlers)
            {
                await handler.InvokeAsync(eventArgs).ConfigureAwait(false);
            }
        }

        public void RemoveHandler(Func<TEventArgs, Task> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.RemoveAll(h => h.WrapsHandler(handler));
        }

        public void RemoveHandler(Action<TEventArgs> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _handlers.RemoveAll(h => h.WrapsHandler(handler));
        }

        public async Task TryInvokeAsync(TEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            try
            {
                await InvokeAsync(eventArgs).ConfigureAwait(false);
            }
            catch (Exception)
            {

            }
        }
    }
}
