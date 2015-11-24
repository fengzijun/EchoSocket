using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

using ALAZ.SystemEx;
using ALAZ.SystemEx.NetEx.SocketsEx;

namespace ChatCryptService
{

    public class ChatCryptService : BaseCryptoService
    {

        #region OnSymmetricAuthenticate

        public override void OnSymmetricAuthenticate(ISocketConnection connection, out RSACryptoServiceProvider serverKey)
        {

            /*
             * A RSACryptoServiceProvider is needed to encrypt and send session key.
             * In server side you need public and private key to decrypt session key.
             * In client side tou need only public key to encrypt session key.
             * 
             * You can create a RSACryptoServiceProvider from a string (file, registry), a CspParameters or a certificate.
             * The following certificate and instructions is in CertificateCreation folder.
             * 
            */

            serverKey = new RSACryptoServiceProvider();

            //----- Using string!
            if (connection.Host.HostType == HostType.htClient)
            {
                serverKey.FromXmlString("<RSAKeyValue><Modulus>z2ksxSTLHSBjY4+IEz7TZU5EclOql5pphA9+xyNQ6c1rYW6VPAmXmiXZKmsza8N++YVLAGnzR95iYyr4oL+mBz8lbhjDH2iqyQL7utbW1s87WaDC2o+82dLnLvwEqBhWpnz4tC0v0kCKayH6Jj+30l3xLdgDwReWF7YEvp6yq6nGxHOeSiioPpTtJzNhWjKGnK6oSZqthfWHewlRl2hVIrewD+JbP5JYTp/7iYptOiCwNAUZEBxODR2743D56J1AeHNc8VpZNvE3ZozIoRFhnxZw0ZpvMbgPliKPyjPeOvOFeqZUJ2zkQ7sH+gnqt67QzkOzznfuFPmTpBo0tMheyw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
            }
            else
            {
                serverKey.FromXmlString("<RSAKeyValue><Modulus>z2ksxSTLHSBjY4+IEz7TZU5EclOql5pphA9+xyNQ6c1rYW6VPAmXmiXZKmsza8N++YVLAGnzR95iYyr4oL+mBz8lbhjDH2iqyQL7utbW1s87WaDC2o+82dLnLvwEqBhWpnz4tC0v0kCKayH6Jj+30l3xLdgDwReWF7YEvp6yq6nGxHOeSiioPpTtJzNhWjKGnK6oSZqthfWHewlRl2hVIrewD+JbP5JYTp/7iYptOiCwNAUZEBxODR2743D56J1AeHNc8VpZNvE3ZozIoRFhnxZw0ZpvMbgPliKPyjPeOvOFeqZUJ2zkQ7sH+gnqt67QzkOzznfuFPmTpBo0tMheyw==</Modulus><Exponent>AQAB</Exponent><P>7IhXSag5zlV+Ary/KDsMinK2Jah/WdTov6Z2XAAPHB4zOGEbhCXdgTEkIrOJNpyobF6L7mR9sTnuV5pr+vWklKkYMbxUEK+KRYo4knUvxx5ED4lFE3KUGeVz6jJ1LY5FqmQT4RTtfwZa6dxRPSgn19/k6sOqyPnnalPz30CYFAk=</P><Q>4Hs/u3UIH+CB3yf2gpupXw5yxl82YX/GuB+ZIAYopM65UlukzFl8eW1iEu42gG/UOpjfmDje+wEvIZ5gcKGjGdDgRmEbAYKNt7X6LqkhIMQqUHt0vAsNrYDXgRFVHdd8YisZ62DzAyMM9nu6v0jPTmhlJSDJwpH3s9XbVy0rmTM=</Q><DP>IF7UW087ggJvOV6tZosWP0hNpz+1Fg0uQTQ91H9pkfaMGfYoNuCbvNeF033wlFnCLvqNefWkwgFknfaTOogtmu69UektNA9iA/xTm6+P91csB1hI7M1seVLOl0mKgc6LuDL0CYS8r/qlrIWrVIxPT5rjkEFw+QpCYmnU4UPMzEk=</DP><DQ>jy7OBfmuBvcin35UBBbZv6Htn45Xl3TzAbpV51FGV2jsWBXQVe+2L5WPeteqt92clwuvgt6zi5LDx0PH68+NwweyJfIGUb4+OrG+NEj4snetLcyxNsguHz8RNmghzHkIA23OiI48MwIGYKmnAh+k6zQ3X6k8R/jm8DQ2RbKwHnU=</DQ><InverseQ>Jrbm5MzTpYI9f0jQKBFzdEdI4DeUFou4BrFpJaheh/+jhzogia+0VsK1CfuXbXgFLPV2aXpQeZYZTX/ANJEymJsp9kAELknq8O+qz6QFyfY0F4Q5H6SVuI/U40XlstYZ2ZEvjGMhXpSAnQUIZ8HJQf8nFOSoAK+HyDwPdvn5RlE=</InverseQ><D>L5hkBK1nyrxG8m7afAgbvJCUVmPqrrVpZzujDRGGnNBdxtL4ffl5h48N4ZUODLmk5p920ZZ+lExs6XLP8Rtpfxo3fadDB28eWdhMadipHkwZw3yHml4HqTijgn2kl+pV4Ainjbkc0zOqT+FRJPvUM/sIwEtkuSevcqt7NT73ozp9roswv0QHBrclCVIN0uiCqPEsfTaLeVEpg48dOh8as6l1XDlgnDGTFjkj2AgFfD27POPE3n4pJSaYJc5zNijbwrjyz8qa1nr+xBQ+yvteNDOg/1LAczP1xrypDgsl/bRHmkljYhPj40SXwK2jwyicgfgCbE3wi6O9t52D8koacQ==</D></RSAKeyValue>");
            }

        }

        #endregion

    }

}
