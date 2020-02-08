﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeeShine.ComponentModel.Data
{
    public class ResponseResult
    {
        public int Code
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public object Data
        {
            get;
            set;
        }
    }
}
