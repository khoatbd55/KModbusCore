using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service.Data.Mutile
{
    public class EventHandleMultileService_Base
    {
        public ETypeEventMultileService Type
        {
            get { return this.type; }
        }

        public string NameComport
        {
            get { return this.nameComport; }
        }

        protected ETypeEventMultileService type;
        protected string nameComport;

    }
}
