namespace Utf8Json.Internal.DoubleConversion;

internal struct Double
{
	public const ulong kSignMask = 9223372036854775808uL;

	public const ulong kExponentMask = 9218868437227405312uL;

	public const ulong kSignificandMask = 4503599627370495uL;

	public const ulong kHiddenBit = 4503599627370496uL;

	public const int kPhysicalSignificandSize = 52;

	public const int kSignificandSize = 53;

	private const int kExponentBias = 1075;

	private const int kDenormalExponent = -1074;

	private const int kMaxExponent = 972;

	private const ulong kInfinity = 9218868437227405312uL;

	private const ulong kNaN = 9221120237041090560uL;

	private ulong d64_;

	public Double(double d)
	{
		UnionDoubleULong unionDoubleULong = new UnionDoubleULong
		{
			d = d
		};
		this.d64_ = unionDoubleULong.u64;
	}

	public Double(DiyFp d)
	{
		this.d64_ = Double.DiyFpToUint64(d);
	}

	public DiyFp AsDiyFp()
	{
		return new DiyFp(this.Significand(), this.Exponent());
	}

	public DiyFp AsNormalizedDiyFp()
	{
		ulong num = this.Significand();
		int num2 = this.Exponent();
		while ((num & 0x10000000000000L) == 0L)
		{
			num <<= 1;
			num2--;
		}
		num <<= 11;
		num2 -= 11;
		return new DiyFp(num, num2);
	}

	public ulong AsUint64()
	{
		return this.d64_;
	}

	public double NextDouble()
	{
		if (this.d64_ == 9218868437227405312L)
		{
			return new Double(9.218868437227405E+18).value();
		}
		if (this.Sign() < 0 && this.Significand() == 0L)
		{
			return 0.0;
		}
		if (this.Sign() < 0)
		{
			return new Double(this.d64_ - 1).value();
		}
		return new Double(this.d64_ + 1).value();
	}

	public double PreviousDouble()
	{
		if (this.d64_ == 18442240474082181120uL)
		{
			return 0.0 - Double.Infinity();
		}
		if (this.Sign() < 0)
		{
			return new Double(this.d64_ + 1).value();
		}
		if (this.Significand() == 0L)
		{
			return -0.0;
		}
		return new Double(this.d64_ - 1).value();
	}

	public int Exponent()
	{
		if (this.IsDenormal())
		{
			return -1074;
		}
		return (int)((this.AsUint64() & 0x7FF0000000000000L) >> 52) - 1075;
	}

	public ulong Significand()
	{
		ulong num = this.AsUint64() & 0xFFFFFFFFFFFFFL;
		if (!this.IsDenormal())
		{
			return num + 4503599627370496L;
		}
		return num;
	}

	public bool IsDenormal()
	{
		return (this.AsUint64() & 0x7FF0000000000000L) == 0;
	}

	public bool IsSpecial()
	{
		return (this.AsUint64() & 0x7FF0000000000000L) == 9218868437227405312L;
	}

	public bool IsNan()
	{
		ulong num = this.AsUint64();
		if ((num & 0x7FF0000000000000L) == 9218868437227405312L)
		{
			return (num & 0xFFFFFFFFFFFFFL) != 0;
		}
		return false;
	}

	public bool IsInfinite()
	{
		ulong num = this.AsUint64();
		if ((num & 0x7FF0000000000000L) == 9218868437227405312L)
		{
			return (num & 0xFFFFFFFFFFFFFL) == 0;
		}
		return false;
	}

	public int Sign()
	{
		if ((this.AsUint64() & 0x8000000000000000uL) != 0L)
		{
			return -1;
		}
		return 1;
	}

	public DiyFp UpperBoundary()
	{
		return new DiyFp(this.Significand() * 2 + 1, this.Exponent() - 1);
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

	public bool LowerBoundaryIsCloser()
	{
		if ((this.AsUint64() & 0xFFFFFFFFFFFFFL) == 0)
		{
			return this.Exponent() != -1074;
		}
		return false;
	}

	public double value()
	{
		UnionDoubleULong unionDoubleULong = new UnionDoubleULong
		{
			u64 = this.d64_
		};
		return unionDoubleULong.d;
	}

	public static int SignificandSizeForOrderOfMagnitude(int order)
	{
		if (order >= -1021)
		{
			return 53;
		}
		if (order <= -1074)
		{
			return 0;
		}
		return order - -1074;
	}

	public static double Infinity()
	{
		return new Double(9.218868437227405E+18).value();
	}

	public static double NaN()
	{
		return new Double(9.221120237041091E+18).value();
	}

	public static ulong DiyFpToUint64(DiyFp diy_fp)
	{
		ulong num = diy_fp.f;
		int num2 = diy_fp.e;
		while (num > 9007199254740991L)
		{
			num >>= 1;
			num2++;
		}
		if (num2 >= 972)
		{
			return 9218868437227405312uL;
		}
		if (num2 < -1074)
		{
			return 0uL;
		}
		while (num2 > -1074 && (num & 0x10000000000000L) == 0L)
		{
			num <<= 1;
			num2--;
		}
		ulong num3 = (ulong)((num2 != -1074 || (num & 0x10000000000000L) != 0L) ? (num2 + 1075) : 0);
		return (num & 0xFFFFFFFFFFFFFL) | (num3 << 52);
	}
}
