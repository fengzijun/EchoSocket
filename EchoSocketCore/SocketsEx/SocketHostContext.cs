using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketHostContext
    {
        public int SocketBufferSize { get; set; }


        public int MessageBufferSize { get; set; }


        public byte[] Delimiter { get; set; }


        public DelimiterType DelimiterType { get; set; }


        public int IdleCheckInterval { get; set; }


        public int IdleTimeOutValue { get; set; }


        public HostType HostType { get; set; }

    }
}
