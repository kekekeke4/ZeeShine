using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.ComponentModel.ServiceHub
{
    /// <summary>
    /// 
    /// </summary>
    public class ServiceExportAttribute : Attribute
    {
        public ServiceExportAttribute(params Type[] contracts)
        {
            Contracts = contracts;
        }

        public IEnumerable<Type> Contracts
        {
            get;
            private set;
        }
    }
}
