using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketRSACryptoProvider:ISocketSecurityProvider
    {
        private BaseSocketConnection connection;
        private AuthMessage am;
        private byte[] buffer;
        public SocketRSACryptoProvider(BaseSocketConnection connection,AuthMessage am,byte[] buffer)
        {
            this.connection = connection;
            this.am = am;
            this.buffer = buffer;
        }


        private SymmetricAlgorithm GetAlgorithm()
        {
            RSACryptoServiceProvider serverPrivateKey;
            connection.Context.Creator.Context.CryptoService.OnSymmetricAuthenticate(connection, out serverPrivateKey);

            SymmetricAlgorithm sa = CryptUtils.CreateSymmetricAlgoritm(connection.Context.Creator.Context.EncryptType);
            sa.Key = serverPrivateKey.Decrypt(am.SessionKey, true);
            sa.IV = serverPrivateKey.Decrypt(am.SessionIV, true);

            return sa;
        }

        public ICryptoTransform CreateEncryptor()
        {

            return GetAlgorithm().CreateEncryptor();
            
        }

        public ICryptoTransform CreateDecryptor()
        {

            return GetAlgorithm().CreateDecryptor();
        }

        public bool Verify()
        {
            RSACryptoServiceProvider clientPublicKey = new RSACryptoServiceProvider();

            clientPublicKey.FromXmlString(Encoding.UTF8.GetString(CryptUtils.DecryptDataForAuthenticate(connection.Context.Decryptor, am.ClientKey)));

            MemoryStream m = new MemoryStream();

            m.Write(buffer, 0, buffer.Length);

            m.Position = 0;

            m.SetLength(0);

            m.Write(am.SessionKey, 0, am.SessionKey.Length);

            m.Write(am.ClientKey, 0, am.ClientKey.Length);

            m.Write(am.Data, 0, am.Data.Length);

            am.SessionIV.Initialize();

            am.SessionKey.Initialize();

            am.ClientKey.Initialize();

            return clientPublicKey.VerifyData(CryptUtils.EncryptDataForAuthenticate(connection.Context.Encryptor, m.ToArray()), "SHA256", am.Sign);

          
        }
    }
}
