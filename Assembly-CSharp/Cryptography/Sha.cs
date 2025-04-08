using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using NorthwoodLib.Pools;

namespace Cryptography
{
	public static class Sha
	{
		public static byte[] Sha1(byte[] message)
		{
			byte[] array;
			using (SHA1 sha = SHA1.Create())
			{
				array = sha.ComputeHash(message);
			}
			return array;
		}

		public static byte[] Sha1(byte[] message, int offset, int length)
		{
			byte[] array;
			using (SHA1 sha = SHA1.Create())
			{
				array = sha.ComputeHash(message, offset, length);
			}
			return array;
		}

		public static byte[] Sha1(string message)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(message.Length));
			int bytes = Utf8.GetBytes(message, array);
			byte[] array2 = Sha.Sha1(array, 0, bytes);
			ArrayPool<byte>.Shared.Return(array, false);
			return array2;
		}

		public static byte[] Sha256(byte[] message)
		{
			byte[] array;
			using (SHA256 sha = SHA256.Create())
			{
				array = sha.ComputeHash(message);
			}
			return array;
		}

		public static byte[] Sha256(byte[] message, int offset, int length)
		{
			byte[] array;
			using (SHA256 sha = SHA256.Create())
			{
				array = sha.ComputeHash(message, offset, length);
			}
			return array;
		}

		public static byte[] Sha256(string message)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(message.Length));
			int bytes = Utf8.GetBytes(message, array);
			byte[] array2 = Sha.Sha256(array, 0, bytes);
			ArrayPool<byte>.Shared.Return(array, false);
			return array2;
		}

		public static byte[] Sha256Hmac(byte[] key, byte[] message)
		{
			byte[] array;
			using (HMACSHA256 hmacsha = new HMACSHA256(key))
			{
				array = hmacsha.ComputeHash(message);
			}
			return array;
		}

		public static byte[] Sha512(string message)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(message.Length));
			int bytes = Utf8.GetBytes(message, array);
			byte[] array2 = Sha.Sha512(array, 0, bytes);
			ArrayPool<byte>.Shared.Return(array, false);
			return array2;
		}

		public static byte[] Sha512(byte[] message)
		{
			byte[] array;
			using (SHA512 sha = SHA512.Create())
			{
				array = sha.ComputeHash(message);
			}
			return array;
		}

		public static byte[] Sha512(byte[] message, int offset, int length)
		{
			byte[] array;
			using (SHA512 sha = SHA512.Create())
			{
				array = sha.ComputeHash(message, offset, length);
			}
			return array;
		}

		public static byte[] Sha512Hmac(byte[] key, byte[] message)
		{
			byte[] array;
			using (HMACSHA512 hmacsha = new HMACSHA512(key))
			{
				array = hmacsha.ComputeHash(message);
			}
			return array;
		}

		public static byte[] Sha512Hmac(byte[] key, string data)
		{
			byte[] array = null;
			byte[] array2;
			try
			{
				array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetByteCount(data));
				int bytes = Utf8.GetBytes(data, array);
				array2 = Sha.Sha512Hmac(key, 0, bytes, array);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array, false);
			}
			return array2;
		}

		public static byte[] Sha512Hmac(byte[] key, int offset, int length, byte[] message)
		{
			byte[] array;
			using (HMACSHA512 hmacsha = new HMACSHA512(key))
			{
				array = hmacsha.ComputeHash(message, offset, length);
			}
			return array;
		}

		public static string HashToString(byte[] hash)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			foreach (byte b in hash)
			{
				stringBuilder.Append(b.ToString("X2"));
			}
			string text = stringBuilder.ToString();
			StringBuilderPool.Shared.Return(stringBuilder);
			return text;
		}
	}
}
