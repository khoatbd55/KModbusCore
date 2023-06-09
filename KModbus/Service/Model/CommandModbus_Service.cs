using KModbus.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Model
{
    public class CommandModbus_Service
    {
        public enum CommandType
        {
            Repeat=0,   // lệnh lắp lại
            NoRepeat    // lệnh không lặp
        }

        public CommandModbus_Service()
        {
            Type = CommandType.NoRepeat;
        }

        public CommandModbus_Service(IModbusRequest request, CommandType Type)
        {
            this.ModbusRequest = request;
            this.Type = Type;
            this.Id= Guid.NewGuid();   
        }

        public CommandModbus_Service(IModbusRequest request, CommandType Type,Guid id)
        {
            this.ModbusRequest = request;
            this.Type = Type;
            this.Id = id;
        }


        public IModbusRequest ModbusRequest { get; set; }
        public CommandType Type { get; set; }
        public Guid Id { get;private set; }
    }
}
