using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Cryptography
{
	public static class Md
	{
		public static byte[] Md5(byte[] message)
		{
			byte[] array;
			using (MD5 md = MD5.Create())
			{
				array = md.ComputeHash(message);
			}
			return array;
		}

		public static byte[] Md5(byte[] message, int offset, int length)
		{
			byte[] array;
			using (MD5 md = MD5.Create())
			{
				array = md.ComputeHash(message, offset, length);
			}
			return array;
		}

		public static byte[] Md5(string message)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(message.Length));
			int bytes = Utf8.GetBytes(message, array);
			byte[] array2 = Md.Md5(array, 0, bytes);
			ArrayPool<byte>.Shared.Return(array, false);
			return array2;
		}
	}
}
