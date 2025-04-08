using System;
using System.Buffers.Binary;

namespace Query
{
	public readonly struct QueryMessage
	{
		public QueryMessage(string payload, uint sequentialNumber, byte queryContentType)
		{
			this = new QueryMessage(Utf8.GetBytes(payload), sequentialNumber, queryContentType, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		}

		public QueryMessage(byte[] payload, uint sequentialNumber, byte queryContentType)
		{
			this = new QueryMessage(payload, sequentialNumber, queryContentType, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		}

		public QueryMessage(string payload, uint sequentialNumber, byte queryContentType, long timestamp)
		{
			this = new QueryMessage(Utf8.GetBytes(payload), sequentialNumber, queryContentType, timestamp);
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
			return this.SequentialNumber == lastRxSequentialNumber + 1U && Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - this.Timestamp) <= (long)timeTolerance;
		}

		public int SerializedSize
		{
			get
			{
				return this.Payload.Length + 13;
			}
		}

		public override string ToString()
		{
			return Utf8.GetString(this.Payload);
		}

		public unsafe int Serialize(Span<byte> buffer)
		{
			BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0, 4), this.SequentialNumber);
			BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(4, 8), this.Timestamp);
			*buffer[12] = this.QueryContentType;
			this.Payload.CopyTo(buffer.Slice(13));
			return this.SerializedSize;
		}

		public unsafe static QueryMessage Deserialize(ReadOnlySpan<byte> buffer)
		{
			uint num = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(0, 4));
			long num2 = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(4, 8));
			return new QueryMessage(buffer.Slice(13).ToArray(), num, *buffer[12], num2);
		}

		public readonly byte QueryContentType;

		public readonly byte[] Payload;

		public readonly uint SequentialNumber;

		public readonly long Timestamp;

		private const int HeaderSize = 13;

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
	}
}
