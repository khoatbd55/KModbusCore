using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.TaskExtentions
{
    public class KAsyncQueueDequeueResult<TItem>
    {
        public KAsyncQueueDequeueResult(bool isSuccess, TItem item)
        {
            IsSuccess = isSuccess;
            Item = item;
        }

        public bool IsSuccess { get; }

        public TItem Item { get; }
    }
}
