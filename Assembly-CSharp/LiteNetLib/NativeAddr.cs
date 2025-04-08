using System;

namespace LiteNetLib
{
	internal readonly struct NativeAddr : IEquatable<NativeAddr>
	{
		public NativeAddr(byte[] address, int len)
		{
			this._part1 = BitConverter.ToInt64(address, 0);
			this._part2 = BitConverter.ToInt64(address, 8);
			if (len > 16)
			{
				this._part3 = BitConverter.ToInt64(address, 16);
				this._part4 = BitConverter.ToInt32(address, 24);
			}
			else
			{
				this._part3 = 0L;
				this._part4 = 0;
			}
			this._hash = (int)(this._part1 >> 32) ^ (int)this._part1 ^ (int)(this._part2 >> 32) ^ (int)this._part2 ^ (int)(this._part3 >> 32) ^ (int)this._part3 ^ this._part4;
		}

		public override int GetHashCode()
		{
			return this._hash;
		}

		public bool Equals(NativeAddr other)
		{
			return this._part1 == other._part1 && this._part2 == other._part2 && this._part3 == other._part3 && this._part4 == other._part4;
		}

		public override bool Equals(object obj)
		{
			if (obj is NativeAddr)
			{
				NativeAddr nativeAddr = (NativeAddr)obj;
				return this.Equals(nativeAddr);
			}
			return false;
		}

		public static bool operator ==(NativeAddr left, NativeAddr right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(NativeAddr left, NativeAddr right)
		{
			return !left.Equals(right);
		}

		private readonly long _part1;

		private readonly long _part2;

		private readonly long _part3;

		private readonly int _part4;

		private readonly int _hash;
	}
}
