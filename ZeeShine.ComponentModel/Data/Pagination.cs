using System;
using System.Collections.Generic;

namespace ZeeShine.ComponentModel.Data
{
    public class Pagination<T>
    {
        public List<T> Items
        {
            get;
            set;
        }

        public long PageIndex
        {
            get;
            set;
        }

        public long PerPageSize
        {
            get;
            set;
        }

        public long TotalSize
        {
            get;
            set;
        }

        public long TotalPage
        {
            get
            {
                if (PerPageSize == 0)
                {
                    return 0;
                }

                return TotalSize / PerPageSize;
            }
        }
    }
}
