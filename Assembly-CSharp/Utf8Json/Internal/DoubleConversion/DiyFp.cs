namespace Utf8Json.Internal.DoubleConversion;

internal struct DiyFp
{
	public const int kSignificandSize = 64;

	public const ulong kUint64MSB = 9223372036854775808uL;

	public ulong f;

	public int e;

	public DiyFp(ulong significand, int exponent)
	{
		this.f = significand;
		this.e = exponent;
	}

	public void Subtract(ref DiyFp other)
	{
		this.f -= other.f;
	}

	public static DiyFp Minus(ref DiyFp a, ref DiyFp b)
	{
		DiyFp result = a;
		result.Subtract(ref b);
		return result;
	}

	public static DiyFp operator -(DiyFp lhs, DiyFp rhs)
	{
		return DiyFp.Minus(ref lhs, ref rhs);
	}

	public void Multiply(ref DiyFp other)
	{
		ulong num = this.f >> 32;
		ulong num2 = this.f & 0xFFFFFFFFu;
		ulong num3 = other.f >> 32;
		ulong num4 = other.f & 0xFFFFFFFFu;
		ulong num5 = num * num3;
		ulong num6 = num2 * num3;
		ulong num7 = num * num4;
		ulong num8 = (num2 * num4 >> 32) + (num7 & 0xFFFFFFFFu) + (num6 & 0xFFFFFFFFu);
		num8 += 2147483648u;
		ulong num9 = num5 + (num7 >> 32) + (num6 >> 32) + (num8 >> 32);
		this.e += other.e + 64;
		this.f = num9;
	}

	public static DiyFp Times(ref DiyFp a, ref DiyFp b)
	{
		DiyFp result = a;
		result.Multiply(ref b);
		return result;
	}

	public static DiyFp operator *(DiyFp lhs, DiyFp rhs)
	{
		return DiyFp.Times(ref lhs, ref rhs);
	}

	public void Normalize()
	{
		ulong num = this.f;
		int num2 = this.e;
		while ((num & 0xFFC0000000000000uL) == 0L)
		{
			num <<= 10;
			num2 -= 10;
		}
		while ((num & 0x8000000000000000uL) == 0L)
		{
			num <<= 1;
			num2--;
		}
		this.f = num;
		this.e = num2;
	}

	public static DiyFp Normalize(ref DiyFp a)
	{
		DiyFp result = a;
		result.Normalize();
		return result;
	}
}
