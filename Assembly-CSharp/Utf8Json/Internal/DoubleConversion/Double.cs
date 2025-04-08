using System;

namespace Utf8Json.Internal.DoubleConversion
{
	internal struct Double
	{
		public Double(double d)
		{
			this.d64_ = new UnionDoubleULong
			{
				d = d
			}.u64;
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
			while ((num & 4503599627370496UL) == 0UL)
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
			if (this.d64_ == 9218868437227405312UL)
			{
				return new Double(9.218868437227405E+18).value();
			}
			if (this.Sign() < 0 && this.Significand() == 0UL)
			{
				return 0.0;
			}
			if (this.Sign() < 0)
			{
				return new Double(this.d64_ - 1UL).value();
			}
			return new Double(this.d64_ + 1UL).value();
		}

		public double PreviousDouble()
		{
			if (this.d64_ == 18442240474082181120UL)
			{
				return -Double.Infinity();
			}
			if (this.Sign() < 0)
			{
				return new Double(this.d64_ + 1UL).value();
			}
			if (this.Significand() == 0UL)
			{
				return -0.0;
			}
			return new Double(this.d64_ - 1UL).value();
		}

		public int Exponent()
		{
			if (this.IsDenormal())
			{
				return -1074;
			}
			return (int)((this.AsUint64() & 9218868437227405312UL) >> 52) - 1075;
		}

		public ulong Significand()
		{
			ulong num = this.AsUint64() & 4503599627370495UL;
			if (!this.IsDenormal())
			{
				return num + 4503599627370496UL;
			}
			return num;
		}

		public bool IsDenormal()
		{
			return (this.AsUint64() & 9218868437227405312UL) == 0UL;
		}

		public bool IsSpecial()
		{
			return (this.AsUint64() & 9218868437227405312UL) == 9218868437227405312UL;
		}

		public bool IsNan()
		{
			ulong num = this.AsUint64();
			return (num & 9218868437227405312UL) == 9218868437227405312UL && (num & 4503599627370495UL) > 0UL;
		}

		public bool IsInfinite()
		{
			ulong num = this.AsUint64();
			return (num & 9218868437227405312UL) == 9218868437227405312UL && (num & 4503599627370495UL) == 0UL;
		}

		public int Sign()
		{
			if ((this.AsUint64() & 9223372036854775808UL) != 0UL)
			{
				return -1;
			}
			return 1;
		}

		public DiyFp UpperBoundary()
		{
			return new DiyFp(this.Significand() * 2UL + 1UL, this.Exponent() - 1);
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

		public bool LowerBoundaryIsCloser()
		{
			return (this.AsUint64() & 4503599627370495UL) == 0UL && this.Exponent() != -1074;
		}

		public double value()
		{
			return new UnionDoubleULong
			{
				u64 = this.d64_
			}.d;
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
			while (num > 9007199254740991UL)
			{
				num >>= 1;
				num2++;
			}
			if (num2 >= 972)
			{
				return 9218868437227405312UL;
			}
			if (num2 < -1074)
			{
				return 0UL;
			}
			while (num2 > -1074 && (num & 4503599627370496UL) == 0UL)
			{
				num <<= 1;
				num2--;
			}
			ulong num3;
			if (num2 == -1074 && (num & 4503599627370496UL) == 0UL)
			{
				num3 = 0UL;
			}
			else
			{
				num3 = (ulong)((long)(num2 + 1075));
			}
			return (num & 4503599627370495UL) | (num3 << 52);
		}

		public const ulong kSignMask = 9223372036854775808UL;

		public const ulong kExponentMask = 9218868437227405312UL;

		public const ulong kSignificandMask = 4503599627370495UL;

		public const ulong kHiddenBit = 4503599627370496UL;

		public const int kPhysicalSignificandSize = 52;

		public const int kSignificandSize = 53;

		private const int kExponentBias = 1075;

		private const int kDenormalExponent = -1074;

		private const int kMaxExponent = 972;

		private const ulong kInfinity = 9218868437227405312UL;

		private const ulong kNaN = 9221120237041090560UL;

		private ulong d64_;
	}
}
