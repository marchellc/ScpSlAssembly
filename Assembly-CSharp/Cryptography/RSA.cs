using System;
using System.Buffers;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Cryptography
{
	public static class RSA
	{
		public static bool Verify(string data, string signature, string key)
		{
			bool flag;
			using (TextReader textReader = new StringReader(key))
			{
				AsymmetricKeyParameter asymmetricKeyParameter = (AsymmetricKeyParameter)new PemReader(textReader).ReadObject();
				ISigner signer = SignerUtilities.GetSigner("SHA256withRSA");
				signer.Init(false, asymmetricKeyParameter);
				byte[] array = Convert.FromBase64String(signature);
				byte[] array2 = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(data.Length));
				int bytes = Utf8.GetBytes(data, array2);
				signer.BlockUpdate(array2, 0, bytes);
				ArrayPool<byte>.Shared.Return(array2, false);
				flag = signer.VerifySignature(array);
			}
			return flag;
		}
	}
}
