using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus
{
    public static class ModbusFunctionCodes
    {
        public const byte ReadCoilStatus = 1;

        public const byte ReadInputStauts = 2;

        public const byte ReadHoldingRegisters = 3;

        public const byte ReadInputRegisters = 4;

        public const byte WriteSingleCoil = 5;

        public const byte PresetSingleRegister = 6;

        public const byte Diagnostics = 8;

        public const ushort DiagnosticsReturnQueryData = 0;

        public const byte ForceMutiplsCoils = 15;

        public const byte PresetMutipleRegister = 16;

        public const byte ReadGenernal = 20;

        public const byte WriteGenernal = 21;

        public const byte ReadWriteRegister = 23;

        public const byte ExceptionOffset = 128;
    }
}
