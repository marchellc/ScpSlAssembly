using System;
using System.Buffers;
using System.IO;
using System.Text;
using GameCore;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using UnityEngine;

namespace Cryptography
{
	public static class ECDSA
	{
		public static AsymmetricCipherKeyPair GenerateKeys(int size = 384)
		{
			ECKeyPairGenerator eckeyPairGenerator = new ECKeyPairGenerator("ECDSA");
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(new SecureRandom(), size);
			eckeyPairGenerator.Init(keyGenerationParameters);
			return eckeyPairGenerator.GenerateKeyPair();
		}

		public static string Sign(string data, AsymmetricKeyParameter privKey)
		{
			return Convert.ToBase64String(ECDSA.SignBytes(data, privKey));
		}

		public static byte[] SignBytes(string data, AsymmetricKeyParameter privKey)
		{
			byte[] array3;
			try
			{
				byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(data.Length));
				int bytes = Utf8.GetBytes(data, array);
				byte[] array2 = ECDSA.SignBytes(array, 0, bytes, privKey);
				ArrayPool<byte>.Shared.Return(array, false);
				array3 = array2;
			}
			catch
			{
				array3 = null;
			}
			return array3;
		}

		public static byte[] SignBytes(byte[] data, AsymmetricKeyParameter privKey)
		{
			return ECDSA.SignBytes(data, 0, data.Length, privKey);
		}

		public static byte[] SignBytes(byte[] data, int offset, int count, AsymmetricKeyParameter privKey)
		{
			byte[] array;
			try
			{
				ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
				signer.Init(true, privKey);
				signer.BlockUpdate(data, offset, count);
				array = signer.GenerateSignature();
			}
			catch
			{
				array = null;
			}
			return array;
		}

		public static bool Verify(string data, string signature, AsymmetricKeyParameter pubKey)
		{
			return ECDSA.VerifyBytes(data, Convert.FromBase64String(signature), pubKey);
		}

		public static bool VerifyBytes(string data, byte[] signature, AsymmetricKeyParameter pubKey)
		{
			bool flag;
			try
			{
				ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
				signer.Init(false, pubKey);
				byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(data.Length));
				int bytes = Utf8.GetBytes(data, array);
				signer.BlockUpdate(array, 0, bytes);
				ArrayPool<byte>.Shared.Return(array, false);
				flag = signer.VerifySignature(signature);
			}
			catch (Exception ex)
			{
				global::GameCore.Console.AddLog("ECDSA Verification Error (BouncyCastle): " + ex.Message + ", " + ex.StackTrace, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
				flag = false;
			}
			return flag;
		}

		public static AsymmetricKeyParameter PublicKeyFromString(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return null;
			}
			AsymmetricKeyParameter asymmetricKeyParameter;
			using (TextReader textReader = new StringReader(key))
			{
				asymmetricKeyParameter = (AsymmetricKeyParameter)new PemReader(textReader).ReadObject();
			}
			return asymmetricKeyParameter;
		}

		public static AsymmetricKeyParameter PrivateKeyFromString(string key)
		{
			AsymmetricKeyParameter @private;
			using (TextReader textReader = new StringReader(key))
			{
				@private = ((AsymmetricCipherKeyPair)new PemReader(textReader).ReadObject()).Private;
			}
			return @private;
		}

		public static string KeyToString(AsymmetricKeyParameter key)
		{
			string text;
			using (TextWriter textWriter = new StringWriter())
			{
				PemWriter pemWriter = new PemWriter(textWriter);
				pemWriter.WriteObject(key);
				pemWriter.Writer.Flush();
				text = textWriter.ToString();
			}
			return text;
		}
	}
}
