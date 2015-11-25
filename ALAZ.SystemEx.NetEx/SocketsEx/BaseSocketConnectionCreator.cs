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
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EchoSocketCore.SocketsEx
{

    /// <summary>
    /// Connection creator using in BaseSocketConnectionHost.
    /// </summary>
    public abstract class BaseSocketConnectionCreator : BaseDisposable, IBaseSocketConnectionCreator
    {

        #region Fields

        //----- Local endpoint of creator!
        private IPEndPoint FLocalEndPoint;

        //----- Host!
        private BaseSocketConnectionHost FHost;
        private string FName;

        private EncryptType FEncryptType;
        private CompressionType FCompressionType;

        private ICryptoService FCryptoService;

        #endregion

        #region Constructor

        public BaseSocketConnectionCreator(BaseSocketConnectionHost host, string name, IPEndPoint localEndPoint, EncryptType encryptType, CompressionType compressionType, ICryptoService cryptoService)
        {

            FHost = host;
            FName = name;
            FLocalEndPoint = localEndPoint;
            FCompressionType = compressionType;
            FEncryptType = encryptType;

            FCryptoService = cryptoService;

        }

        #endregion

        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {

            FLocalEndPoint = null;
            FCryptoService = null;
            FHost = null;

            base.Free(canAccessFinalizable);

        }

        #endregion

        #region Methods

        #region ValidateServerCertificateCallback

        internal bool ValidateServerCertificateCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {

            bool acceptCertificate = false;
            FCryptoService.OnSSLClientValidateServerCertificate(certificate, chain, sslPolicyErrors, out acceptCertificate);
            
            return acceptCertificate;

        }

        #endregion

        #region Abstract Methods

        public abstract void Start();
        public abstract void Stop();

        #endregion

        #endregion

        #region Properties

        internal BaseSocketConnectionHost Host
        {
            get { return FHost; }
        }

        public string Name
        {
          get { return FName; }
        }

        public ICryptoService CryptoService
        {
            get { return FCryptoService; }
            set { FCryptoService = value; } 
        }

        public EncryptType EncryptType
        {
            get { return FEncryptType; }
            set { FEncryptType = value; }
        }

        internal IPEndPoint InternalLocalEndPoint
        {
            get { return FLocalEndPoint; }
            set { FLocalEndPoint = value; }
        }

        public CompressionType CompressionType
        {
            get { return FCompressionType; }
            set { FCompressionType = value; }
        }

        #endregion

    }

}
