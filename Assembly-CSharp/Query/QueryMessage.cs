using System;
using System.Buffers.Binary;

namespace Query;

public readonly struct QueryMessage
{
	public enum ServerReceivedContentType : byte
	{
		Command
	}

	public enum ClientReceivedContentType : byte
	{
		ConsoleString,
		CommandException,
		QueryMessage,
		RemoteAdminSerializedResponse,
		RemoteAdminPlaintextResponse,
		RemoteAdminUnsuccessfulPlaintextResponse
	}

	public readonly byte QueryContentType;

	public readonly byte[] Payload;

	public readonly uint SequentialNumber;

	public readonly long Timestamp;

	private const int HeaderSize = 13;

	public int SerializedSize => this.Payload.Length + 13;

	public QueryMessage(string payload, uint sequentialNumber, byte queryContentType)
		: this(Utf8.GetBytes(payload), sequentialNumber, queryContentType, DateTimeOffset.UtcNow.ToUnixTimeSeconds())
	{
	}

	public QueryMessage(byte[] payload, uint sequentialNumber, byte queryContentType)
		: this(payload, sequentialNumber, queryContentType, DateTimeOffset.UtcNow.ToUnixTimeSeconds())
	{
	}

	public QueryMessage(string payload, uint sequentialNumber, byte queryContentType, long timestamp)
		: this(Utf8.GetBytes(payload), sequentialNumber, queryContentType, timestamp)
	{
	}

	public QueryMessage(byte[] payload, uint sequentialNumber, byte queryContentType, long timestamp)
	{
		this.Payload = payload;
		this.SequentialNumber = sequentialNumber;
		this.QueryContentType = queryContentType;
		this.Timestamp = timestamp;
	}

	public bool Validate(uint lastRxSequentialNumber, int timeTolerance = 120)
	{
		if (this.SequentialNumber == lastRxSequentialNumber + 1)
		{
			return Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - this.Timestamp) <= timeTolerance;
		}
		return false;
	}

	public override string ToString()
	{
		return Utf8.GetString(this.Payload);
	}

	public int Serialize(Span<byte> buffer)
	{
		BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), this.SequentialNumber);
		BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(4, 8), this.Timestamp);
		buffer[12] = this.QueryContentType;
		this.Payload.CopyTo(buffer.Slice(13));
		return this.SerializedSize;
	}

	public static QueryMessage Deserialize(ReadOnlySpan<byte> buffer)
	{
		uint sequentialNumber = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(0, 4));
		long timestamp = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(4, 8));
		return new QueryMessage(buffer.Slice(13).ToArray(), sequentialNumber, buffer[12], timestamp);
	}
}
