using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EchoSocketCore.SocketsEx;

namespace EchoCryptService
{
    public class EchoCryptService : BaseCryptoService
    {
        #region OnSymmetricAuthenticate

        public override void OnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {
            /*
             * A RSACryptoServiceProvider is needed to encrypt and send session key.
             * In server side you need public and private key to decrypt session key.
             * In client side tou need only public key to encrypt session key.
             *
             * You can create a RSACryptoServiceProvider from
             *      - a string (file, registry)
             *      - a certificate (from file)
             *      - a certificate (from certificate store)
             *
             * The following certificate and instructions is in CertificateCreation folder.
            */

            /*
            //----- Using string!
            serverKey = new RSACryptoServiceProvider();

            if (connection.Host.HostType == HostType.htClient)
            {
                serverKey.FromXmlString("<RSAKeyValue><Modulus>z2ksxSTLHSBjY4+IEz7TZU5EclOql5pphA9+xyNQ6c1rYW6VPAmXmiXZKmsza8N++YVLAGnzR95iYyr4oL+mBz8lbhjDH2iqyQL7utbW1s87WaDC2o+82dLnLvwEqBhWpnz4tC0v0kCKayH6Jj+30l3xLdgDwReWF7YEvp6yq6nGxHOeSiioPpTtJzNhWjKGnK6oSZqthfWHewlRl2hVIrewD+JbP5JYTp/7iYptOiCwNAUZEBxODR2743D56J1AeHNc8VpZNvE3ZozIoRFhnxZw0ZpvMbgPliKPyjPeOvOFeqZUJ2zkQ7sH+gnqt67QzkOzznfuFPmTpBo0tMheyw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
            }
            else
            {
                serverKey.FromXmlString("<RSAKeyValue><Modulus>z2ksxSTLHSBjY4+IEz7TZU5EclOql5pphA9+xyNQ6c1rYW6VPAmXmiXZKmsza8N++YVLAGnzR95iYyr4oL+mBz8lbhjDH2iqyQL7utbW1s87WaDC2o+82dLnLvwEqBhWpnz4tC0v0kCKayH6Jj+30l3xLdgDwReWF7YEvp6yq6nGxHOeSiioPpTtJzNhWjKGnK6oSZqthfWHewlRl2hVIrewD+JbP5JYTp/7iYptOiCwNAUZEBxODR2743D56J1AeHNc8VpZNvE3ZozIoRFhnxZw0ZpvMbgPliKPyjPeOvOFeqZUJ2zkQ7sH+gnqt67QzkOzznfuFPmTpBo0tMheyw==</Modulus><Exponent>AQAB</Exponent><P>7IhXSag5zlV+Ary/KDsMinK2Jah/WdTov6Z2XAAPHB4zOGEbhCXdgTEkIrOJNpyobF6L7mR9sTnuV5pr+vWklKkYMbxUEK+KRYo4knUvxx5ED4lFE3KUGeVz6jJ1LY5FqmQT4RTtfwZa6dxRPSgn19/k6sOqyPnnalPz30CYFAk=</P><Q>4Hs/u3UIH+CB3yf2gpupXw5yxl82YX/GuB+ZIAYopM65UlukzFl8eW1iEu42gG/UOpjfmDje+wEvIZ5gcKGjGdDgRmEbAYKNt7X6LqkhIMQqUHt0vAsNrYDXgRFVHdd8YisZ62DzAyMM9nu6v0jPTmhlJSDJwpH3s9XbVy0rmTM=</Q><DP>IF7UW087ggJvOV6tZosWP0hNpz+1Fg0uQTQ91H9pkfaMGfYoNuCbvNeF033wlFnCLvqNefWkwgFknfaTOogtmu69UektNA9iA/xTm6+P91csB1hI7M1seVLOl0mKgc6LuDL0CYS8r/qlrIWrVIxPT5rjkEFw+QpCYmnU4UPMzEk=</DP><DQ>jy7OBfmuBvcin35UBBbZv6Htn45Xl3TzAbpV51FGV2jsWBXQVe+2L5WPeteqt92clwuvgt6zi5LDx0PH68+NwweyJfIGUb4+OrG+NEj4snetLcyxNsguHz8RNmghzHkIA23OiI48MwIGYKmnAh+k6zQ3X6k8R/jm8DQ2RbKwHnU=</DQ><InverseQ>Jrbm5MzTpYI9f0jQKBFzdEdI4DeUFou4BrFpJaheh/+jhzogia+0VsK1CfuXbXgFLPV2aXpQeZYZTX/ANJEymJsp9kAELknq8O+qz6QFyfY0F4Q5H6SVuI/U40XlstYZ2ZEvjGMhXpSAnQUIZ8HJQf8nFOSoAK+HyDwPdvn5RlE=</InverseQ><D>L5hkBK1nyrxG8m7afAgbvJCUVmPqrrVpZzujDRGGnNBdxtL4ffl5h48N4ZUODLmk5p920ZZ+lExs6XLP8Rtpfxo3fadDB28eWdhMadipHkwZw3yHml4HqTijgn2kl+pV4Ainjbkc0zOqT+FRJPvUM/sIwEtkuSevcqt7NT73ozp9roswv0QHBrclCVIN0uiCqPEsfTaLeVEpg48dOh8as6l1XDlgnDGTFjkj2AgFfD27POPE3n4pJSaYJc5zNijbwrjyz8qa1nr+xBQ+yvteNDOg/1LAczP1xrypDgsl/bRHmkljYhPj40SXwK2jwyicgfgCbE3wi6O9t52D8koacQ==</D></RSAKeyValue>");
            }
            */

            /*
            //----- Using Certificate Store!
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2 certificate = store.Certificates.Find(X509FindType.FindBySubjectName, "TESTCERT", true)[0];
            store.Close();

            serverKey = new RSACryptoServiceProvider();

            if (connection.Host.HostType == HostType.htClient)
            {
                serverKey = (RSACryptoServiceProvider)certificate.PublicKey.Key;
            }
            else
            {
                //----- Need ProviderType=24 for SHA256 sign!
                RSACryptoServiceProvider certKey = (RSACryptoServiceProvider)certificate.PrivateKey;

                CspParameters csp = new CspParameters();
                csp.ProviderName = null;
                csp.ProviderType = 24;
                csp.KeyContainerName = certKey.CspKeyContainerInfo.KeyContainerName;

                serverKey = new RSACryptoServiceProvider(csp);
            }
            */

            //----- Using pfx/p12 certificate!
            X509Certificate2 certificate;

            if (connection.Context.HostType == HostType.htClient)
            {
                certificate = new X509Certificate2(@"..\..\..\..\CertificateCreation\cert.crt");

                serverKey = new RSACryptoServiceProvider();
                serverKey = (RSACryptoServiceProvider)certificate.PublicKey.Key;
            }
            else
            {
                certificate = new X509Certificate2(@"..\..\..\..\CertificateCreation\cert.p12", "12345");

                //----- Need ProviderType=24 for SHA256 sign!
                RSACryptoServiceProvider certKey = (RSACryptoServiceProvider)certificate.PrivateKey;

                CspParameters csp = new CspParameters();
                csp.ProviderName = null;
                csp.ProviderType = 24;
                csp.KeyContainerName = certKey.CspKeyContainerInfo.KeyContainerName;

                serverKey = new RSACryptoServiceProvider(csp);

                certKey.Clear();
            }
        }

        #endregion OnSymmetricAuthenticate

        #region OnSSLServerAuthenticate

        public override void OnSSLServerAuthenticate(ISocketConnection connection, out X509Certificate2 certificate, out bool clientAuthenticate, ref bool checkRevocation)
        {
            //----- Set server certificate, client authentication and certificate revocation!
            //----- Look at the CertificateCreation folder for instructions

            /*
            //----- Using Certificate Store!
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            certificate = store.Certificates.Find(X509FindType.FindBySubjectName, "TESTCERT", false)[0];

            store.Close();
            */

            //----- Using pfx/p12 file!
            certificate = new X509Certificate2(@"..\..\..\..\CertificateCreation\cert.p12", "12345");

            clientAuthenticate = false;
            checkRevocation = false;
        }

        #endregion OnSSLServerAuthenticate

        #region OnSSLClientAuthenticate

        public override void OnSSLClientAuthenticate(ISocketConnection connection, out string serverName, ref X509Certificate2Collection certs, ref bool checkRevocation)
        {
            /*
            //----- Using client certificate!
            //----- The following certificate and instructions is in CertificateCreation folder.

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            certs = store.Certificates.Find(X509FindType.FindBySubjectName, serverName, false);

            store.Close();
            */

            //----- Check server certificate!
            serverName = "TESTCERT";
            checkRevocation = false;
        }

        #endregion OnSSLClientAuthenticate

        #region OnSSLClientValidateServerCertificate

        public override void OnSSLClientValidateServerCertificate(X509Certificate serverCertificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, out bool acceptCertificate)
        {
            foreach (X509ChainElement element in chain.ChainElements)
            {
                Console.WriteLine("Element issuer name: {0}", element.Certificate.Issuer);
                Console.WriteLine("Element certificate valid until: {0}", element.Certificate.NotAfter);
                Console.WriteLine("Element certificate is valid: {0}", element.Certificate.Verify());
                Console.WriteLine("Element error status length: {0}", element.ChainElementStatus.Length);
                Console.WriteLine("Element information: {0}", element.Information);
                Console.WriteLine("Number of element extensions: {0}{1}", element.Certificate.Extensions.Count, Environment.NewLine);

                if (chain.ChainStatus.Length > 1)
                {
                    for (int index = 0; index < element.ChainElementStatus.Length; index++)
                    {
                        Console.WriteLine(element.ChainElementStatus[index].Status);
                        Console.WriteLine(element.ChainElementStatus[index].StatusInformation);
                    }
                }
            }

            acceptCertificate = true;
        }

        #endregion OnSSLClientValidateServerCertificate
    }
}