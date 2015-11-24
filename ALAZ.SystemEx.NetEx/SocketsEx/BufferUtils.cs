/* ====================================================================
 * Copyright (c) 2009 Andre Luis Azevedo (az.andrel@yahoo.com.br)
 * All rights reserved.
 *                       
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *    In addition, the source code must keep original namespace names.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution. In addition, the binary form must keep the original 
 *    namespace names and original file name.
 * 
 * 3. The name "ALAZ" or "ALAZ Library" must not be used to endorse or promote 
 *    products derived from this software without prior written permission.
 *
 * 4. Products derived from this software may not be called "ALAZ" or
 *    "ALAZ Library" nor may "ALAZ" or "ALAZ Library" appear in their 
 *    names without prior written permission of the author.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY
 * EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR
 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE. 
 */

using System;
using System.Net.Sockets;

namespace ALAZ.SystemEx.NetEx.SocketsEx
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

                    result = connection.BaseHost.BufferManager.TakeBuffer(bufferSize);
                    Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                    

                    break;

                case DelimiterType.dtMessageTailExcludeOnReceive:
                case DelimiterType.dtMessageTailIncludeOnReceive:

                    if (connection.Delimiter != null && connection.Delimiter.Length >= 0)
                    {

                        //----- Need delimiter!
                        bufferSize = buffer.Length + connection.Delimiter.Length;

                        result = connection.BaseHost.BufferManager.TakeBuffer(bufferSize);
                        Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                        Buffer.BlockCopy(connection.Delimiter, 0, result, buffer.Length, connection.Delimiter.Length);

                    }
                    else
                    {
                        bufferSize = buffer.Length;

                        result = connection.BaseHost.BufferManager.TakeBuffer(bufferSize);
                        Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
                    }

                    break;

            }
                    
            return result;

        }

        #endregion

        #region GetRawBuffer

        public static byte[] GetRawBuffer(BaseSocketConnection connection, byte[] buffer, int readBytes)
        {

            byte[] result = new byte[readBytes];
            Buffer.BlockCopy(buffer, 0, result, 0, readBytes);
            return result;

        }

        #endregion

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

        #endregion

    }

}
