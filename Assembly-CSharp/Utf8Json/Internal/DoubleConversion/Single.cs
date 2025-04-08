using System;

namespace Utf8Json.Internal.DoubleConversion
{
	internal struct Single
	{
		public Single(float f)
		{
			this.d32_ = new UnionFloatUInt
			{
				f = f
			}.u32;
		}

		public DiyFp AsDiyFp()
		{
			return new DiyFp((ulong)this.Significand(), this.Exponent());
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
			return (int)(((this.AsUint32() & 2139095040U) >> 23) - 150U);
		}

		public uint Significand()
		{
			uint num = this.AsUint32() & 8388607U;
			if (!this.IsDenormal())
			{
				return num + 8388608U;
			}
			return num;
		}

		public bool IsDenormal()
		{
			return (this.AsUint32() & 2139095040U) == 0U;
		}

		public bool IsSpecial()
		{
			return (this.AsUint32() & 2139095040U) == 2139095040U;
		}

		public bool IsNan()
		{
			uint num = this.AsUint32();
			return (num & 2139095040U) == 2139095040U && (num & 8388607U) > 0U;
		}

		public bool IsInfinite()
		{
			uint num = this.AsUint32();
			return (num & 2139095040U) == 2139095040U && (num & 8388607U) == 0U;
		}

		public int Sign()
		{
			if ((this.AsUint32() & 2147483648U) != 0U)
			{
				return -1;
			}
			return 1;
		}

		public void NormalizedBoundaries(out DiyFp out_m_minus, out DiyFp out_m_plus)
		{
			DiyFp diyFp = this.AsDiyFp();
			DiyFp diyFp2 = new DiyFp((diyFp.f << 1) + 1UL, diyFp.e - 1);
			DiyFp diyFp3 = DiyFp.Normalize(ref diyFp2);
			DiyFp diyFp4;
			if (this.LowerBoundaryIsCloser())
			{
				diyFp4 = new DiyFp((diyFp.f << 2) - 1UL, diyFp.e - 2);
			}
			else
			{
				diyFp4 = new DiyFp((diyFp.f << 1) - 1UL, diyFp.e - 1);
			}
			diyFp4.f <<= diyFp4.e - diyFp3.e;
			diyFp4.e = diyFp3.e;
			out_m_plus = diyFp3;
			out_m_minus = diyFp4;
		}

		public DiyFp UpperBoundary()
		{
			return new DiyFp((ulong)(this.Significand() * 2U + 1U), this.Exponent() - 1);
		}

		public bool LowerBoundaryIsCloser()
		{
			return (this.AsUint32() & 8388607U) == 0U && this.Exponent() != -149;
		}

		public float value()
		{
			return new UnionFloatUInt
			{
				u32 = this.d32_
			}.f;
		}

		public static float Infinity()
		{
			return new Single(2.139095E+09f).value();
		}

		public static float NaN()
		{
			return new Single(2.1432893E+09f).value();
		}

		private const int kExponentBias = 150;

		private const int kDenormalExponent = -149;

		private const int kMaxExponent = 105;

		private const uint kInfinity = 2139095040U;

		private const uint kNaN = 2143289344U;

		public const uint kSignMask = 2147483648U;

		public const uint kExponentMask = 2139095040U;

		public const uint kSignificandMask = 8388607U;

		public const uint kHiddenBit = 8388608U;

		public const int kPhysicalSignificandSize = 23;

		public const int kSignificandSize = 24;

		private uint d32_;
	}
}
