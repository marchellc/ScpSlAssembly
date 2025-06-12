using System;

namespace LiteNetLib.Utils;

public class NtpPacket
{
	private static readonly DateTime Epoch = new DateTime(1900, 1, 1);

	public byte[] Bytes { get; }

	public NtpLeapIndicator LeapIndicator => (NtpLeapIndicator)((this.Bytes[0] & 0xC0) >> 6);

	public int VersionNumber
	{
		get
		{
			return (this.Bytes[0] & 0x38) >> 3;
		}
		private set
		{
			this.Bytes[0] = (byte)((this.Bytes[0] & -57) | (value << 3));
		}
	}

	public NtpMode Mode
	{
		get
		{
			return (NtpMode)(this.Bytes[0] & 7);
		}
		private set
		{
			this.Bytes[0] = (byte)((uint)(this.Bytes[0] & -8) | (uint)value);
		}
	}

	public int Stratum => this.Bytes[1];

	public int Poll => this.Bytes[2];

	public int Precision => (sbyte)this.Bytes[3];

	public TimeSpan RootDelay => this.GetTimeSpan32(4);

	public TimeSpan RootDispersion => this.GetTimeSpan32(8);

	public uint ReferenceId => this.GetUInt32BE(12);

	public DateTime? ReferenceTimestamp => this.GetDateTime64(16);

	public DateTime? OriginTimestamp => this.GetDateTime64(24);

	public DateTime? ReceiveTimestamp => this.GetDateTime64(32);

	public DateTime? TransmitTimestamp
	{
		get
		{
			return this.GetDateTime64(40);
		}
		private set
		{
			this.SetDateTime64(40, value);
		}
	}

	public DateTime? DestinationTimestamp { get; private set; }

	public TimeSpan RoundTripTime
	{
		get
		{
			this.CheckTimestamps();
			return this.ReceiveTimestamp.Value - this.OriginTimestamp.Value + (this.DestinationTimestamp.Value - this.TransmitTimestamp.Value);
		}
	}

	public TimeSpan CorrectionOffset
	{
		get
		{
			this.CheckTimestamps();
			return TimeSpan.FromTicks((this.ReceiveTimestamp.Value - this.OriginTimestamp.Value - (this.DestinationTimestamp.Value - this.TransmitTimestamp.Value)).Ticks / 2);
		}
	}

	public NtpPacket()
		: this(new byte[48])
	{
		this.Mode = NtpMode.Client;
		this.VersionNumber = 4;
		this.TransmitTimestamp = DateTime.UtcNow;
	}

	internal NtpPacket(byte[] bytes)
	{
		if (bytes.Length < 48)
		{
			throw new ArgumentException("SNTP reply packet must be at least 48 bytes long.", "bytes");
		}
		this.Bytes = bytes;
	}

	public static NtpPacket FromServerResponse(byte[] bytes, DateTime destinationTimestamp)
	{
		return new NtpPacket(bytes)
		{
			DestinationTimestamp = destinationTimestamp
		};
	}

	internal void ValidateRequest()
	{
		if (this.Mode != NtpMode.Client)
		{
			throw new InvalidOperationException("This is not a request SNTP packet.");
		}
		if (this.VersionNumber == 0)
		{
			throw new InvalidOperationException("Protocol version of the request is not specified.");
		}
		if (!this.TransmitTimestamp.HasValue)
		{
			throw new InvalidOperationException("TransmitTimestamp must be set in request packet.");
		}
	}

	internal void ValidateReply()
	{
		if (this.Mode != NtpMode.Server)
		{
			throw new InvalidOperationException("This is not a reply SNTP packet.");
		}
		if (this.VersionNumber == 0)
		{
			throw new InvalidOperationException("Protocol version of the reply is not specified.");
		}
		if (this.Stratum == 0)
		{
			throw new InvalidOperationException($"Received Kiss-o'-Death SNTP packet with code 0x{this.ReferenceId:x}.");
		}
		if (this.LeapIndicator == NtpLeapIndicator.AlarmCondition)
		{
			throw new InvalidOperationException("SNTP server has unsynchronized clock.");
		}
		this.CheckTimestamps();
	}

	private void CheckTimestamps()
	{
		if (!this.OriginTimestamp.HasValue)
		{
			throw new InvalidOperationException("Origin timestamp is missing.");
		}
		if (!this.ReceiveTimestamp.HasValue)
		{
			throw new InvalidOperationException("Receive timestamp is missing.");
		}
		if (!this.TransmitTimestamp.HasValue)
		{
			throw new InvalidOperationException("Transmit timestamp is missing.");
		}
		if (!this.DestinationTimestamp.HasValue)
		{
			throw new InvalidOperationException("Destination timestamp is missing.");
		}
	}

	private DateTime? GetDateTime64(int offset)
	{
		ulong uInt64BE = this.GetUInt64BE(offset);
		if (uInt64BE == 0L)
		{
			return null;
		}
		DateTime epoch = NtpPacket.Epoch;
		return new DateTime(epoch.Ticks + Convert.ToInt64((double)uInt64BE * 0.0023283064365386963));
	}

	private void SetDateTime64(int offset, DateTime? value)
	{
		long value2;
		if (value.HasValue)
		{
			long ticks = value.Value.Ticks;
			DateTime epoch = NtpPacket.Epoch;
			value2 = (long)Convert.ToUInt64((double)(ticks - epoch.Ticks) * 429.4967296);
		}
		else
		{
			value2 = 0L;
		}
		this.SetUInt64BE(offset, (ulong)value2);
	}

	private TimeSpan GetTimeSpan32(int offset)
	{
		return TimeSpan.FromSeconds((double)this.GetInt32BE(offset) / 65536.0);
	}

	private ulong GetUInt64BE(int offset)
	{
		return NtpPacket.SwapEndianness(BitConverter.ToUInt64(this.Bytes, offset));
	}

	private void SetUInt64BE(int offset, ulong value)
	{
		FastBitConverter.GetBytes(this.Bytes, offset, NtpPacket.SwapEndianness(value));
	}

	private int GetInt32BE(int offset)
	{
		return (int)this.GetUInt32BE(offset);
	}

	private uint GetUInt32BE(int offset)
	{
		return NtpPacket.SwapEndianness(BitConverter.ToUInt32(this.Bytes, offset));
	}

	private static uint SwapEndianness(uint x)
	{
		return ((x & 0xFF) << 24) | ((x & 0xFF00) << 8) | ((x & 0xFF0000) >> 8) | ((x & 0xFF000000u) >> 24);
	}

	private static ulong SwapEndianness(ulong x)
	{
		return ((ulong)NtpPacket.SwapEndianness((uint)x) << 32) | NtpPacket.SwapEndianness((uint)(x >> 32));
	}
}
