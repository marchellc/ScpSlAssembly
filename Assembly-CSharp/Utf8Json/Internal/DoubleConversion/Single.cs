namespace Utf8Json.Internal.DoubleConversion;

internal struct Single
{
	private const int kExponentBias = 150;

	private const int kDenormalExponent = -149;

	private const int kMaxExponent = 105;

	private const uint kInfinity = 2139095040u;

	private const uint kNaN = 2143289344u;

	public const uint kSignMask = 2147483648u;

	public const uint kExponentMask = 2139095040u;

	public const uint kSignificandMask = 8388607u;

	public const uint kHiddenBit = 8388608u;

	public const int kPhysicalSignificandSize = 23;

	public const int kSignificandSize = 24;

	private uint d32_;

	public Single(float f)
	{
		UnionFloatUInt unionFloatUInt = new UnionFloatUInt
		{
			f = f
		};
		this.d32_ = unionFloatUInt.u32;
	}

	public DiyFp AsDiyFp()
	{
		return new DiyFp(this.Significand(), this.Exponent());
	}

	public uint AsUint32()
	{
		return this.d32_;
	}

	public int Exponent()
	{
		if (this.IsDenormal())
		{
			return -149;
		}
		return (int)(((this.AsUint32() & 0x7F800000) >> 23) - 150);
	}

	public uint Significand()
	{
		uint num = this.AsUint32() & 0x7FFFFF;
		if (!this.IsDenormal())
		{
			return num + 8388608;
		}
		return num;
	}

	public bool IsDenormal()
	{
		return (this.AsUint32() & 0x7F800000) == 0;
	}

	public bool IsSpecial()
	{
		return (this.AsUint32() & 0x7F800000) == 2139095040;
	}

	public bool IsNan()
	{
		uint num = this.AsUint32();
		if ((num & 0x7F800000) == 2139095040)
		{
			return (num & 0x7FFFFF) != 0;
		}
		return false;
	}

	public bool IsInfinite()
	{
		uint num = this.AsUint32();
		if ((num & 0x7F800000) == 2139095040)
		{
			return (num & 0x7FFFFF) == 0;
		}
		return false;
	}

	public int Sign()
	{
		if ((this.AsUint32() & 0x80000000u) != 0)
		{
			return -1;
		}
		return 1;
	}

	public void NormalizedBoundaries(out DiyFp out_m_minus, out DiyFp out_m_plus)
	{
		DiyFp diyFp = this.AsDiyFp();
		DiyFp a = new DiyFp((diyFp.f << 1) + 1, diyFp.e - 1);
		DiyFp diyFp2 = DiyFp.Normalize(ref a);
		DiyFp diyFp3 = ((!this.LowerBoundaryIsCloser()) ? new DiyFp((diyFp.f << 1) - 1, diyFp.e - 1) : new DiyFp((diyFp.f << 2) - 1, diyFp.e - 2));
		diyFp3.f <<= diyFp3.e - diyFp2.e;
		diyFp3.e = diyFp2.e;
		out_m_plus = diyFp2;
		out_m_minus = diyFp3;
	}

	public DiyFp UpperBoundary()
	{
		return new DiyFp(this.Significand() * 2 + 1, this.Exponent() - 1);
	}

	public bool LowerBoundaryIsCloser()
	{
		if ((this.AsUint32() & 0x7FFFFF) == 0)
		{
			return this.Exponent() != -149;
		}
		return false;
	}

	public float value()
	{
		UnionFloatUInt unionFloatUInt = new UnionFloatUInt
		{
			u32 = this.d32_
		};
		return unionFloatUInt.f;
	}

	public static float Infinity()
	{
		return new Single(2.139095E+09f).value();
	}

	public static float NaN()
	{
		return new Single(2.1432893E+09f).value();
	}
}
