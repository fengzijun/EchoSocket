using System;
using System.Net.Sockets;

namespace EchoSocketCore.SocketsEx
{
    internal class BufferUtils
    {
        #region GetPacketBuffer

        public static byte[] GetPacketBuffer(BaseSocketConnection connection, byte[] buffer, ref int bufferSize)
        {
            byte[] result = null;
            buffer = CryptUtils.EncryptData(connection, buffer);

            switch (connection.DelimiterType)
            {
                case DelimiterType.dtNone:

                    //----- No Delimiter!
                    bufferSize = buffer.Length;

                    result = connection.Context.Host.Context.BufferManager.TakeBuffer(bufferSize);
                    Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);

                    break;

                case DelimiterType.dtMessageTailExcludeOnReceive:
                case DelimiterType.dtMessageTailIncludeOnReceive:

                    if (connection.Delimiter != null && connection.Delimiter.Length >= 0)
                    {
                        //----- Need delimiter!
                        bufferSize = buffer.Length + connection.Delimiter.Length;

                        result = connection.Context.Host.Context.BufferManager.TakeBuffer(bufferSize);
                        Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                        Buffer.BlockCopy(connection.Delimiter, 0, result, buffer.Length, connection.Delimiter.Length);
                    }
                    else
                    {
                        bufferSize = buffer.Length;

                        result = connection.Context.Host.Context.BufferManager.TakeBuffer(bufferSize);
                        Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                    }

                    break;
            }

            return result;
        }

        #endregion GetPacketBuffer

        #region GetRawBuffer

        public static byte[] GetRawBuffer(BaseSocketConnection connection, byte[] buffer, int readBytes)
        {
            byte[] result = new byte[readBytes];
            Buffer.BlockCopy(buffer, 0, result, 0, readBytes);
            return result;
        }

        #endregion GetRawBuffer

        #region GetRawBufferWithTail

        public static byte[] GetRawBufferWithTail(BaseSocketConnection connection, SocketAsyncEventArgs e, int position, int delimiterSize)
        {
            //----- Get Raw Buffer with Tail!
            byte[] result = null;

            if (connection.DelimiterType == DelimiterType.dtMessageTailIncludeOnReceive)
            {
                result = new byte[position - e.Offset + 1];
            }
            else
            {
                result = new byte[position - e.Offset + 1 - delimiterSize];
            }

            Buffer.BlockCopy(e.Buffer, e.Offset, result, 0, result.Length);

            for (int i = 0; i < delimiterSize; i++)
            {
                e.Buffer[position - i] = 0;
            }

            return result;
        }

        #endregion GetRawBufferWithTail
    }
}