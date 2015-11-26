using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace EchoSocketCore.SocketsEx
{
    /// <summary>
    /// Crypt tools.
    /// </summary>
    internal static class CryptUtils
    {
        #region CreateSymmetricAlgoritm

        /// <summary>
        /// Creates an asymmetric algoritm.
        /// </summary>
        /// <param name="encryptType">
        /// Encrypt type.
        /// </param>
        public static SymmetricAlgorithm CreateSymmetricAlgoritm(EncryptType encryptType)
        {
            SymmetricAlgorithm result = null;

            switch (encryptType)
            {
                case EncryptType.etRijndael:
                    {
                        result = new RijndaelManaged();

                        result.KeySize = 256;
                        result.BlockSize = 256;

                        break;
                    }
            }

            if (result != null)
            {
                result.Mode = CipherMode.CBC;
            }

            return result;
        }

        #endregion CreateSymmetricAlgoritm

        #region EncryptDataForAuthenticate

        /// <summary>
        /// Encrypts using default padding.
        /// </summary>
        /// <param name="buffer">
        /// Data to be rncrypted
        /// </param>
        public static byte[] EncryptDataForAuthenticate(ICryptoTransform ct, byte[] buffer)
        {
            byte[] result = null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(buffer, 0, buffer.Length);
                    cs.FlushFinalBlock();

                    result = ms.ToArray();
                }
            }

            return result;
        }

        #endregion EncryptDataForAuthenticate

        #region DecryptDataForAuthenticate

        /// <summary>
        /// Encrypts using default padding.
        /// </summary>
        /// <param name="buffer">
        /// Data to be encrypted
        /// </param>
        public static byte[] DecryptDataForAuthenticate(ICryptoTransform ct, byte[] buffer)
        {
            byte[] result = null;

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Read))
                using (BinaryReader b = new BinaryReader(cs))
                {
                    ms.Position = 0;
                    result = b.ReadBytes(4096);
                }
            }

            return result;
        }

        #endregion DecryptDataForAuthenticate

        #region EncryptData

        /// <summary>
        /// Encrypts the data.
        /// </summary>
        /// <param name="connection">
        /// Connection information.
        /// </param>
        /// <param name="buffer">
        /// Data to be encrypted.
        /// </param>
        /// <param name="signOnly">
        /// Indicates is encrypt method only uses symmetric algoritm.
        /// </param>
        public static byte[] EncryptData(BaseSocketConnection connection, byte[] buffer)
        {
            byte[] result = null;

            if (
                 (connection.EventProcessing == EventProcessing.epEncrypt)
                 || (connection.EventProcessing == EventProcessing.epProxy)
                 || (connection.Context.Creator.Context.EncryptType == EncryptType.etSSL && connection.Context.Creator.Context.CompressionType == CompressionType.ctNone)
                 || (connection.Context.Creator.Context.EncryptType == EncryptType.etNone && connection.Context.Creator.Context.CompressionType == CompressionType.ctNone)
                )
            {
                result = buffer;
            }
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    CryptoStream cs = null;
                    GZipStream gs = null;

                    switch (connection.Context.Creator.Context.EncryptType)
                    {
                        case EncryptType.etNone:
                        case EncryptType.etSSL:
                            {
                                break;
                            }

                        default:
                            {
                                cs = new CryptoStream(ms, connection.Context.Encryptor, CryptoStreamMode.Write);
                                break;
                            }
                    }

                    switch (connection.Context.Creator.Context.CompressionType)
                    {
                        case CompressionType.ctGZIP:
                            {
                                if (cs != null)
                                {
                                    gs = new GZipStream(cs, CompressionMode.Compress, true);
                                }
                                else
                                {
                                    gs = new GZipStream(ms, CompressionMode.Compress, true);
                                }

                                break;
                            }
                    }

                    if (gs != null)
                    {
                        gs.Write(buffer, 0, buffer.Length);
                        gs.Flush();
                        gs.Close();
                    }
                    else
                    {
                        cs.Write(buffer, 0, buffer.Length);
                    }

                    if (cs != null)
                    {
                        cs.FlushFinalBlock();
                        cs.Close();
                    }

                    result = ms.ToArray();
                }
            }

            return result;
        }

        #endregion EncryptData

        #region DecryptData

        /// <summary>
        /// Decrypts the data.
        /// </summary>
        /// <param name="connection">
        /// Connection information.
        /// </param>
        /// <param name="buffer">
        /// Data to be encrypted.
        /// </param>
        /// <param name="maxBufferSize">
        /// Max buffer size accepted.
        /// </param>
        public static byte[] DecryptData(BaseSocketConnection connection, byte[] buffer, int maxBufferSize)
        {
            byte[] result = null;

            if (
                 (connection.EventProcessing == EventProcessing.epEncrypt)
                 || (connection.EventProcessing == EventProcessing.epProxy)
                 || (connection.Context.Creator.Context.EncryptType == EncryptType.etSSL && connection.Context.Creator.Context.CompressionType == CompressionType.ctNone)
                 || (connection.Context.Creator.Context.EncryptType == EncryptType.etNone && connection.Context.Creator.Context.CompressionType == CompressionType.ctNone)

                )
            {
                result = buffer;
            }
            else
            {
                MemoryStream ms = new MemoryStream(buffer);
                CryptoStream cs = null;
                GZipStream gs = null;

                switch (connection.Context.Creator.Context.EncryptType)
                {
                    case EncryptType.etNone:
                    case EncryptType.etSSL:
                        {
                            break;
                        }

                    default:
                        {
                            cs = new CryptoStream(ms, connection.Context.Decryptor, CryptoStreamMode.Read);
                            break;
                        }
                }

                switch (connection.Context.Creator.Context.CompressionType)
                {
                    case CompressionType.ctGZIP:
                        {
                            if (cs != null)
                            {
                                gs = new GZipStream(cs, CompressionMode.Decompress, true);
                            }
                            else
                            {
                                gs = new GZipStream(ms, CompressionMode.Decompress, true);
                            }

                            break;
                        }
                }

                BinaryReader b = null;

                if (gs != null)
                {
                    b = new BinaryReader(gs);
                }
                else
                {
                    b = new BinaryReader(cs);
                }

                result = b.ReadBytes(maxBufferSize);

                b.Close();
            }

            return result;
        }

        #endregion DecryptData
    }
}