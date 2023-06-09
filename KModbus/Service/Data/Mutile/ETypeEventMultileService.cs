using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Mutile
{
    public enum ETypeEventMultileService
    {
        NoResponse=0,
        Response,
        ClosedConnection,
        ExceptionOccur
    }
}
