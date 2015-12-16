using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace EchoSocketCore.SocketsEx
{
    public class SocketRSACryptoProvider : ISocketSecurityProvider
    {
        private BaseSocketConnection connection;

        private byte[] buffer;


        public SocketRSACryptoProvider(BaseSocketConnection connection, byte[] buffer)
        {
            this.connection = connection;
            this.buffer = buffer;
        }



        public MemoryStream EcryptForClient()
        {
            #region Client

            //----- Create authenticate message
            AuthMessage am = new AuthMessage();

            //----- Generate client asymmetric key pair (public and private)
            RSACryptoServiceProvider clientKeyPair = new RSACryptoServiceProvider(2048);

            //----- Get the server public key
            RSACryptoServiceProvider serverPublicKey;
            connection.Context.Creator.Context.CryptoService.OnSymmetricAuthenticate(connection, out serverPublicKey);

            //----- Generates symmetric algoritm
            SymmetricAlgorithm sa = CryptUtils.CreateSymmetricAlgoritm(connection.Context.Creator.Context.EncryptType);

            //----- Adjust connection cryptors
            connection.Context.Encryptor = sa.CreateEncryptor();
            connection.Context.Decryptor = sa.CreateDecryptor();

            //----- Encrypt session IV and session Key with server public key
            am.SessionIV = serverPublicKey.Encrypt(sa.IV, true);
            am.SessionKey = serverPublicKey.Encrypt(sa.Key, true);

            //----- Encrypt client public key with symmetric algorithm
            am.ClientKey = CryptUtils.EncryptDataForAuthenticate(connection.Context.Encryptor, Encoding.UTF8.GetBytes(clientKeyPair.ToXmlString(false)));

            //----- Create hash salt!
            am.Data = new byte[32];
            RNGCryptoServiceProvider.Create().GetBytes(am.Data);

            MemoryStream m = new MemoryStream();

            //----- Create a sign with am.SourceKey, am.SessionKey and am.Data (salt)!
            m.Write(am.SessionKey, 0, am.SessionKey.Length);
            m.Write(am.ClientKey, 0, am.ClientKey.Length);
            m.Write(am.Data, 0, am.Data.Length);

            am.Sign = clientKeyPair.SignData(CryptUtils.EncryptDataForAuthenticate(connection.Context.Encryptor, m.ToArray()), "SHA256");

            //----- Serialize authentication message
            m.SetLength(0);
            new BinaryFormatter().Serialize(m, am);

            //m.Close();

            am.SessionIV.Initialize();
            am.SessionKey.Initialize();

            serverPublicKey.Clear();
            clientKeyPair.Clear();

            return m;

            #endregion Client
        }

        public MemoryStream DecryptForServer()
        {
            MemoryStream m = new MemoryStream();
            m.Write(buffer, 0, buffer.Length);
            m.Position = 0;

            BinaryFormatter b = new BinaryFormatter();

            AuthMessage am = (AuthMessage)b.Deserialize(m);

            if (am == null)
                throw new SymmetricAuthenticationException("Symmetric sign error.");

            RSACryptoServiceProvider serverPrivateKey;
            connection.Context.Creator.Context.CryptoService.OnSymmetricAuthenticate(connection, out serverPrivateKey);

            SymmetricAlgorithm sa = CryptUtils.CreateSymmetricAlgoritm(connection.Context.Creator.Context.EncryptType);
            sa.Key = serverPrivateKey.Decrypt(am.SessionKey, true);
            sa.IV = serverPrivateKey.Decrypt(am.SessionIV, true);

            connection.Context.Encryptor = sa.CreateEncryptor();
            connection.Context.Decryptor = sa.CreateDecryptor();

            RSACryptoServiceProvider clientPublicKey = new RSACryptoServiceProvider();
            clientPublicKey.FromXmlString(Encoding.UTF8.GetString(CryptUtils.DecryptDataForAuthenticate(connection.Context.Decryptor, am.ClientKey)));

            m.SetLength(0);
            m.Write(am.SessionKey, 0, am.SessionKey.Length);
            m.Write(am.ClientKey, 0, am.ClientKey.Length);
            m.Write(am.Data, 0, am.Data.Length);

            am.SessionIV.Initialize();
            am.SessionKey.Initialize();
            am.ClientKey.Initialize();

            if (clientPublicKey.VerifyData(CryptUtils.EncryptDataForAuthenticate(connection.Context.Encryptor, m.ToArray()), "SHA256", am.Sign))
            {
                am.Data = new byte[32];
                RNGCryptoServiceProvider.Create().GetBytes(am.Data);

                am.SessionIV = null;
                am.SessionKey = null;
                am.ClientKey = null;
                am.Sign = serverPrivateKey.SignData(am.Data, "SHA256");

                m.SetLength(0);
                b.Serialize(m, am);
            }
            else
                throw new SymmetricAuthenticationException("Symmetric sign error.");

            am.Sign.Initialize();
            m.Close();

            serverPrivateKey.Clear();
            clientPublicKey.Clear();

            return m;
        }

        public AuthMessage DecryptForClient()
        {
            MemoryStream m = new MemoryStream();
            m.Write(buffer, 0, buffer.Length);
            m.Position = 0;
            BinaryFormatter b = new BinaryFormatter();
            AuthMessage am = (AuthMessage)b.Deserialize(m);

            if (am == null)
                throw new SymmetricAuthenticationException("Symmetric sign error.");

            RSACryptoServiceProvider serverPublicKey;
            connection.Context.Creator.Context.CryptoService.OnSymmetricAuthenticate(connection, out serverPublicKey);

            //----- Verify sign
            if (serverPublicKey.VerifyData(am.Data, "SHA256", am.Sign))
            {
                connection.Context.EventProcessing = EventProcessing.epUser;
                //FireOnConnected(connection);
            }
            else
            {
                throw new SymmetricAuthenticationException("Symmetric sign error.");
            }

            am.Data.Initialize();
            am.Sign.Initialize();

            serverPublicKey.Clear();

            m.Close();

            return am;
        }


    }
}