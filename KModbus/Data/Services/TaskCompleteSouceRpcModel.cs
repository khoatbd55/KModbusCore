using KModbus.Extention;
using KUtilities.TaskExtentions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Data.Services
{
    public class TaskCompleteSouceRpcModel
    {
        public KAsyncTaskCompletionSource<ModbusCmdResponse_Base<ModbusMessage>> TaskCompleteSource { get; private set; }
        public Guid Id { get;private set; }

        public TaskCompleteSouceRpcModel(KAsyncTaskCompletionSource<ModbusCmdResponse_Base<ModbusMessage>> taskCompleteSource, Guid id)
        {
            TaskCompleteSource = taskCompleteSource;
            Id = id;
        }
    }
}
