using System;
using System.Security.Cryptography;
using System.Text;
using GameCore;
using NorthwoodLib.Pools;

public static class RandomGenerator
{
	private static readonly Random Random;

	private static readonly bool CryptoRng;

	private static readonly byte[] OneByte;

	private static readonly byte[] TwoBytes;

	private static readonly byte[] FourBytes;

	private static readonly byte[] EightBytes;

	private static readonly byte[] SixteenBytes;

	private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

	static RandomGenerator()
	{
		RandomGenerator.Random = new Random();
		RandomGenerator.OneByte = new byte[1];
		RandomGenerator.TwoBytes = new byte[2];
		RandomGenerator.FourBytes = new byte[4];
		RandomGenerator.EightBytes = new byte[8];
		RandomGenerator.SixteenBytes = new byte[16];
		RandomGenerator.CryptoRng = ConfigFile.ServerConfig.GetBool("use_crypto_rng");
	}

	public static bool GetBool(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetBoolUnsecure();
		}
		return RandomGenerator.GetBoolSecure();
	}

	private static bool GetBoolSecure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
		return RandomGenerator.OneByte[0] > 127;
	}

	private static bool GetBoolUnsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
		return RandomGenerator.OneByte[0] > 127;
	}

	public static string GetStringSecure(int length)
	{
		if (length <= 0)
		{
			throw new ArgumentException("Length must be greater than 0.", "length");
		}
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent(length);
		try
		{
			for (int i = 0; i < length; i++)
			{
				stringBuilder.Append("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[RandomNumberGenerator.GetInt32(0, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Length)]);
			}
			return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		}
		catch
		{
			StringBuilderPool.Shared.Return(stringBuilder);
			throw;
		}
	}

	public static byte[] GetBytes(int count, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetBytesUnsecure(count);
		}
		return RandomGenerator.GetBytesSecure(count);
	}

	private static byte[] GetBytesSecure(int count)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		byte[] array = new byte[count];
		rNGCryptoServiceProvider.GetBytes(array);
		return array;
	}

	private static byte[] GetBytesUnsecure(int count)
	{
		byte[] array = new byte[count];
		RandomGenerator.Random.NextBytes(array);
		return array;
	}

	public static byte GetByte(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetByteUnsecure();
		}
		return RandomGenerator.GetByteSecure();
	}

	private static byte GetByteSecure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
		return RandomGenerator.OneByte[0];
	}

	private static byte GetByteUnsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
		return RandomGenerator.OneByte[0];
	}

	public static sbyte GetSByte(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetSByteUnsecure();
		}
		return RandomGenerator.GetSByteSecure();
	}

	private static sbyte GetSByteSecure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
		return (sbyte)RandomGenerator.OneByte[0];
	}

	private static sbyte GetSByteUnsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
		return (sbyte)RandomGenerator.OneByte[0];
	}

	public static short GetInt16(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetInt16Unsecure();
		}
		return RandomGenerator.GetInt16Secure();
	}

	private static short GetInt16Secure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.TwoBytes);
		return BitConverter.ToInt16(RandomGenerator.TwoBytes, 0);
	}

	private static short GetInt16Unsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.TwoBytes);
		return BitConverter.ToInt16(RandomGenerator.TwoBytes, 0);
	}

	public static ushort GetUInt16(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetUInt16Unsecure();
		}
		return RandomGenerator.GetUInt16Secure();
	}

	private static ushort GetUInt16Secure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.TwoBytes);
		return BitConverter.ToUInt16(RandomGenerator.TwoBytes, 0);
	}

	private static ushort GetUInt16Unsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.TwoBytes);
		return BitConverter.ToUInt16(RandomGenerator.TwoBytes, 0);
	}

	public static int GetInt32(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetInt32Unsecure();
		}
		return RandomGenerator.GetInt32Secure();
	}

	private static int GetInt32Secure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
		return BitConverter.ToInt32(RandomGenerator.FourBytes, 0);
	}

	private static int GetInt32Unsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
		return BitConverter.ToInt32(RandomGenerator.FourBytes, 0);
	}

	public static uint GetUInt32(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetUInt32Unsecure();
		}
		return RandomGenerator.GetUInt32Secure();
	}

	private static uint GetUInt32Secure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
		return BitConverter.ToUInt32(RandomGenerator.FourBytes, 0);
	}

	private static uint GetUInt32Unsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
		return BitConverter.ToUInt32(RandomGenerator.FourBytes, 0);
	}

	public static long GetInt64(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetInt64Unsecure();
		}
		return RandomGenerator.GetInt64Secure();
	}

	private static long GetInt64Secure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
		return BitConverter.ToInt64(RandomGenerator.EightBytes, 0);
	}

	private static long GetInt64Unsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
		return BitConverter.ToInt64(RandomGenerator.EightBytes, 0);
	}

	public static ulong GetUInt64(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetUInt64Unsecure();
		}
		return RandomGenerator.GetUInt64Secure();
	}

	private static ulong GetUInt64Secure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
		return BitConverter.ToUInt64(RandomGenerator.EightBytes, 0);
	}

	private static ulong GetUInt64Unsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
		return BitConverter.ToUInt64(RandomGenerator.EightBytes, 0);
	}

	public static float GetFloat(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetFloatUnsecure();
		}
		return RandomGenerator.GetFloatSecure();
	}

	private static float GetFloatSecure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
		return BitConverter.ToSingle(RandomGenerator.FourBytes, 0);
	}

	private static float GetFloatUnsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
		return BitConverter.ToSingle(RandomGenerator.FourBytes, 0);
	}

	public static double GetDouble(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetDoubleUnsecure();
		}
		return RandomGenerator.GetDoubleSecure();
	}

	private static double GetDoubleSecure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
		return BitConverter.ToDouble(RandomGenerator.EightBytes, 0);
	}

	private static double GetDoubleUnsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
		return BitConverter.ToDouble(RandomGenerator.EightBytes, 0);
	}

	public static decimal GetDecimal(bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetDecimalUnsecure();
		}
		return RandomGenerator.GetDecimalSecure();
	}

	private static decimal GetDecimalSecure()
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.SixteenBytes);
		return new decimal(new int[4]
		{
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 0),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 4),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 8),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 12)
		});
	}

	private static decimal GetDecimalUnsecure()
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.SixteenBytes);
		return new decimal(new int[4]
		{
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 0),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 4),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 8),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 12)
		});
	}

	public static byte GetByte(byte min, byte max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetByteUnsecure(min, max);
		}
		return RandomGenerator.GetByteSecure(min, max);
	}

	private static byte GetByteSecure(byte min, byte max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
		byte b = RandomGenerator.OneByte[0];
		while (b < min || b >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
			b = RandomGenerator.OneByte[0];
		}
		return b;
	}

	private static byte GetByteUnsecure(byte min, byte max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
		byte b = RandomGenerator.OneByte[0];
		while (b < min || b >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
			b = RandomGenerator.OneByte[0];
		}
		return b;
	}

	public static sbyte GetSByte(sbyte min, sbyte max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetSByteUnsecure(min, max);
		}
		return RandomGenerator.GetSByteSecure(min, max);
	}

	private static sbyte GetSByteSecure(sbyte min, sbyte max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
		sbyte b = (sbyte)RandomGenerator.OneByte[0];
		while (b < min || b >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.OneByte);
			b = (sbyte)RandomGenerator.OneByte[0];
		}
		return b;
	}

	private static sbyte GetSByteUnsecure(sbyte min, sbyte max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
		sbyte b = (sbyte)RandomGenerator.OneByte[0];
		while (b < min || b >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.OneByte);
			b = (sbyte)RandomGenerator.OneByte[0];
		}
		return b;
	}

	public static short GetInt16(short min, short max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetInt16Unsecure(min, max);
		}
		return RandomGenerator.GetInt16Secure(min, max);
	}

	private static short GetInt16Secure(short min, short max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.TwoBytes);
		short num = BitConverter.ToInt16(RandomGenerator.TwoBytes, 0);
		while (num < min || num >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.TwoBytes);
			num = BitConverter.ToInt16(RandomGenerator.TwoBytes, 0);
		}
		return num;
	}

	private static short GetInt16Unsecure(short min, short max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.TwoBytes);
		short num = BitConverter.ToInt16(RandomGenerator.TwoBytes, 0);
		while (num < min || num >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.TwoBytes);
			num = BitConverter.ToInt16(RandomGenerator.TwoBytes, 0);
		}
		return num;
	}

	public static ushort GetUInt16(ushort min, ushort max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetUInt16Unsecure(min, max);
		}
		return RandomGenerator.GetUInt16Secure(min, max);
	}

	private static ushort GetUInt16Secure(ushort min, ushort max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.TwoBytes);
		ushort num = BitConverter.ToUInt16(RandomGenerator.TwoBytes, 0);
		while (num < min || num >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.TwoBytes);
			num = BitConverter.ToUInt16(RandomGenerator.TwoBytes, 0);
		}
		return num;
	}

	private static ushort GetUInt16Unsecure(ushort min, ushort max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.TwoBytes);
		ushort num = BitConverter.ToUInt16(RandomGenerator.TwoBytes, 0);
		while (num < min || num >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.TwoBytes);
			num = BitConverter.ToUInt16(RandomGenerator.TwoBytes, 0);
		}
		return num;
	}

	public static int GetInt32(int min, int max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetInt32Unsecure(min, max);
		}
		return RandomGenerator.GetInt32Secure(min, max);
	}

	private static int GetInt32Secure(int min, int max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
		int num = BitConverter.ToInt32(RandomGenerator.FourBytes, 0);
		while (num < min || num >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
			num = BitConverter.ToInt32(RandomGenerator.FourBytes, 0);
		}
		return num;
	}

	private static int GetInt32Unsecure(int min, int max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
		int num = BitConverter.ToInt32(RandomGenerator.FourBytes, 0);
		while (num < min || num >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
			num = BitConverter.ToInt32(RandomGenerator.FourBytes, 0);
		}
		return num;
	}

	public static uint GetUInt32(uint min, uint max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetUInt32Unsecure(min, max);
		}
		return RandomGenerator.GetUInt32Secure(min, max);
	}

	private static uint GetUInt32Secure(uint min, uint max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
		uint num = BitConverter.ToUInt32(RandomGenerator.FourBytes, 0);
		while (num < min || num >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
			num = BitConverter.ToUInt32(RandomGenerator.FourBytes, 0);
		}
		return num;
	}

	private static uint GetUInt32Unsecure(uint min, uint max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
		uint num = BitConverter.ToUInt32(RandomGenerator.FourBytes, 0);
		while (num < min || num >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
			num = BitConverter.ToUInt32(RandomGenerator.FourBytes, 0);
		}
		return num;
	}

	public static long GetInt64(long min, long max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetInt64Unsecure(min, max);
		}
		return RandomGenerator.GetInt64Secure(min, max);
	}

	private static long GetInt64Secure(long min, long max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
		long num = BitConverter.ToInt64(RandomGenerator.EightBytes, 0);
		while (num < min || num >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
			num = BitConverter.ToInt64(RandomGenerator.EightBytes, 0);
		}
		return num;
	}

	private static long GetInt64Unsecure(long min, long max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
		long num = BitConverter.ToInt64(RandomGenerator.EightBytes, 0);
		while (num < min || num >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
			num = BitConverter.ToInt64(RandomGenerator.EightBytes, 0);
		}
		return num;
	}

	public static ulong GetUInt64(ulong min, ulong max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetUInt64Unsecure(min, max);
		}
		return RandomGenerator.GetUInt64Secure(min, max);
	}

	private static ulong GetUInt64Secure(ulong min, ulong max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
		ulong num = BitConverter.ToUInt64(RandomGenerator.EightBytes, 0);
		while (num < min || num >= max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
			num = BitConverter.ToUInt64(RandomGenerator.EightBytes, 0);
		}
		return num;
	}

	private static ulong GetUInt64Unsecure(ulong min, ulong max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
		ulong num = BitConverter.ToUInt64(RandomGenerator.EightBytes, 0);
		while (num < min || num >= max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
			num = BitConverter.ToUInt64(RandomGenerator.EightBytes, 0);
		}
		return num;
	}

	public static float GetFloat(float min, float max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetFloatUnsecure(min, max);
		}
		return RandomGenerator.GetFloatSecure(min, max);
	}

	private static float GetFloatSecure(float min, float max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
		float num = BitConverter.ToSingle(RandomGenerator.FourBytes, 0);
		while (num < min || num > max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.FourBytes);
			num = BitConverter.ToSingle(RandomGenerator.FourBytes, 0);
		}
		return num;
	}

	private static float GetFloatUnsecure(float min, float max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
		float num = BitConverter.ToSingle(RandomGenerator.FourBytes, 0);
		while (num < min || num > max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.FourBytes);
			num = BitConverter.ToSingle(RandomGenerator.FourBytes, 0);
		}
		return num;
	}

	public static double GetDouble(double min, double max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetDoubleUnsecure(min, max);
		}
		return RandomGenerator.GetDoubleSecure(min, max);
	}

	private static double GetDoubleSecure(double min, double max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
		double num = BitConverter.ToDouble(RandomGenerator.EightBytes, 0);
		while (num < min || num > max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.EightBytes);
			num = BitConverter.ToDouble(RandomGenerator.EightBytes, 0);
		}
		return num;
	}

	private static double GetDoubleUnsecure(double min, double max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
		double num = BitConverter.ToDouble(RandomGenerator.EightBytes, 0);
		while (num < min || num > max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.EightBytes);
			num = BitConverter.ToDouble(RandomGenerator.EightBytes, 0);
		}
		return num;
	}

	public static decimal GetDecimal(decimal min, decimal max, bool secure = false)
	{
		if (!secure && !RandomGenerator.CryptoRng)
		{
			return RandomGenerator.GetDecimalUnsecure(min, max);
		}
		return RandomGenerator.GetDecimalSecure(min, max);
	}

	private static decimal GetDecimalSecure(decimal min, decimal max)
	{
		using RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		rNGCryptoServiceProvider.GetBytes(RandomGenerator.SixteenBytes);
		decimal num = new decimal(new int[4]
		{
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 0),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 4),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 8),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 12)
		});
		while (num < min || num > max)
		{
			rNGCryptoServiceProvider.GetBytes(RandomGenerator.SixteenBytes);
			num = new decimal(new int[4]
			{
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 0),
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 4),
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 8),
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 12)
			});
		}
		return num;
	}

	private static decimal GetDecimalUnsecure(decimal min, decimal max)
	{
		RandomGenerator.Random.NextBytes(RandomGenerator.SixteenBytes);
		decimal num = new decimal(new int[4]
		{
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 0),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 4),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 8),
			BitConverter.ToInt32(RandomGenerator.SixteenBytes, 12)
		});
		while (num < min || num > max)
		{
			RandomGenerator.Random.NextBytes(RandomGenerator.SixteenBytes);
			num = new decimal(new int[4]
			{
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 0),
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 4),
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 8),
				BitConverter.ToInt32(RandomGenerator.SixteenBytes, 12)
			});
		}
		return num;
	}
}
