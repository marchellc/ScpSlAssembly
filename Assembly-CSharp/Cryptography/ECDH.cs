using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace Cryptography
{
	public static class ECDH
	{
		public static AsymmetricCipherKeyPair GenerateKeys(int size = 384)
		{
			ECKeyPairGenerator eckeyPairGenerator = new ECKeyPairGenerator("ECDH");
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(new SecureRandom(), size);
			eckeyPairGenerator.Init(keyGenerationParameters);
			return eckeyPairGenerator.GenerateKeyPair();
		}

		public static ECDHBasicAgreement Init(AsymmetricCipherKeyPair localKey)
		{
			ECDHBasicAgreement ecdhbasicAgreement = new ECDHBasicAgreement();
			ecdhbasicAgreement.Init(localKey.Private);
			return ecdhbasicAgreement;
		}

		public static byte[] DeriveKey(ECDHBasicAgreement exchange, AsymmetricKeyParameter remoteKey)
		{
			byte[] array;
			using (SHA256 sha = SHA256.Create())
			{
				array = sha.ComputeHash(exchange.CalculateAgreement(remoteKey).ToByteArray());
			}
			return array;
		}
	}
}
