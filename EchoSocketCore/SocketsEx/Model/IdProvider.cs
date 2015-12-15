using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EchoSocketCore.SocketsEx.Model
{
    public static class IdProvider
    {
        private static long connnectionId = 10000;

        public static long GetConnectionId()
        {
            Interlocked.Increment(ref connnectionId);
            return connnectionId;
        }
    }
}
