namespace Utf8Json.Internal;

public static class ByteArrayComparer
{
	public static bool Equals(byte[] xs, int xsOffset, int xsCount, byte[] ys)
	{
		if (xs == null || ys == null || xsCount != ys.Length)
		{
			return false;
		}
		for (int i = 0; i < ys.Length; i++)
		{
			if (xs[xsOffset++] != ys[i])
			{
				return false;
			}
		}
		return true;
	}

	public static bool Equals(byte[] xs, int xsOffset, int xsCount, byte[] ys, int ysOffset, int ysCount)
	{
		if (xs == null || ys == null || xsCount != ysCount)
		{
			return false;
		}
		for (int i = 0; i < xsCount; i++)
		{
			if (xs[xsOffset++] != ys[ysOffset++])
			{
				return false;
			}
		}
		return true;
	}
}
