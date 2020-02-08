using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.ComponentModel.Data
{
    public class PaginationQuery
    {
        public int PageIndex
        {
            get;
            set;
        }

        public int PerPageSize
        {
            get;
            set;
        }

        public string Keyword
        {
            get;
            set;
        }

        public string Sort
        {
            get;
            set;
        }
    }
}
