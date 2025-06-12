using System;
using System.Text;

public static class Utf8
{
	private static readonly UTF8Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	public static int GetLength(string data)
	{
		return Utf8.Encoding.GetByteCount(data);
	}

	public static byte[] GetBytes(string data)
	{
		return Utf8.Encoding.GetBytes(data);
	}

	public static int GetBytes(string data, byte[] buffer)
	{
		return Utf8.Encoding.GetBytes(data, 0, data.Length, buffer, 0);
	}

	public static int GetBytes(string data, byte[] buffer, int offset)
	{
		return Utf8.Encoding.GetBytes(data, 0, data.Length, buffer, offset);
	}

	public static string GetString(byte[] data)
	{
		return Utf8.Encoding.GetString(data);
	}

	public static string GetString(byte[] data, int offset, int count)
	{
		return Utf8.Encoding.GetString(data, offset, count);
	}

	public static string GetString(ReadOnlySpan<byte> data)
	{
		return Utf8.Encoding.GetString(data);
	}

	public static string GetString(ReadOnlySpan<byte> data, int offset, int count)
	{
		return Utf8.Encoding.GetString(data.Slice(offset, count));
	}
}
