﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public interface ISocket
    {
      
        SocketContext Context { get; set; }
    }
}
