using System;
using System.Buffers.Binary;

namespace Query
{
	public readonly struct QueryHandshake
	{
		public int SizeToServer
		{
			get
			{
				int num = (this.Flags.HasFlagFast(QueryHandshake.ClientFlags.RestrictPermissions) ? 9 : 0);
				int num2 = (this.Flags.HasFlagFast(QueryHandshake.ClientFlags.SpecifyLogUsername) ? Utf8.GetLength(this.Username) : 0);
				return 35 + num + num2;
			}
		}

		public QueryHandshake(ushort maxPacketSize, long timestamp, byte[] authChallenge, QueryHandshake.ClientFlags flags = QueryHandshake.ClientFlags.None, ulong permissions = 18446744073709551615UL, byte kickPower = 255, string username = null, ushort serverTimeoutThreshold = 0)
		{
			if (authChallenge.Length != 24)
			{
				throw new ArgumentException(string.Format("Auth challenge must be {0} bytes long.", 24), "authChallenge");
			}
			if (flags.HasFlagFast(QueryHandshake.ClientFlags.SpecifyLogUsername) && string.IsNullOrWhiteSpace(username))
			{
				throw new ArgumentException("Username must be specified (and not be empty or whitespace) when ClientFlags.SpecifyLogUsername is set.", "username");
			}
			this.MaxPacketSize = maxPacketSize;
			this.Timestamp = timestamp;
			this.AuthChallenge = authChallenge;
			this.ServerTimeoutThreshold = serverTimeoutThreshold;
			this.Flags = flags;
			this.Permissions = permissions;
			this.KickPower = kickPower;
			this.Username = username;
		}

		public QueryHandshake(ushort maxPacketSize, byte[] authChallenge, QueryHandshake.ClientFlags flags = QueryHandshake.ClientFlags.None, ulong permissions = 18446744073709551615UL, byte kickPower = 255, string username = null, ushort serverTimeoutThreshold = 0)
		{
			this = new QueryHandshake(maxPacketSize, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), authChallenge, flags, permissions, kickPower, username, serverTimeoutThreshold);
		}

		public bool Validate(int timeTolerance = 120)
		{
			return Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - this.Timestamp) <= (long)timeTolerance;
		}

		public int Serialize(Span<byte> buffer, bool toServer)
		{
			BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), this.MaxPacketSize);
			BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(2, 8), this.Timestamp);
			this.AuthChallenge.CopyTo(buffer.Slice(10, 24));
			BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(34, 2), this.ServerTimeoutThreshold);
			return 36;
		}

		public unsafe static QueryHandshake Deserialize(ReadOnlySpan<byte> buffer, bool toServer)
		{
			ushort num = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));
			long num2 = BinaryPrimitives.ReadInt64BigEndian(buffer.Slice(2, 8));
			byte[] array = buffer.Slice(10, 24).ToArray();
			QueryHandshake.ClientFlags clientFlags = (QueryHandshake.ClientFlags)(*buffer[34]);
			ulong num3 = ulong.MaxValue;
			byte b = byte.MaxValue;
			string text = null;
			int num4 = 35;
			if (clientFlags.HasFlagFast(QueryHandshake.ClientFlags.RestrictPermissions))
			{
				num3 = BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(num4, 8));
				num4 += 8;
				b = *buffer[num4++];
			}
			if (clientFlags.HasFlagFast(QueryHandshake.ClientFlags.SpecifyLogUsername))
			{
				text = Utf8.GetString(buffer, num4, buffer.Length - num4);
			}
			return new QueryHandshake(num, num2, array, clientFlags, num3, b, text, 0);
		}

		public readonly ushort MaxPacketSize;

		public readonly long Timestamp;

		public readonly byte[] AuthChallenge;

		public readonly ushort ServerTimeoutThreshold;

		public readonly QueryHandshake.ClientFlags Flags;

		public readonly ulong Permissions;

		public readonly byte KickPower;

		public readonly string Username;

		public const int ChallengeLength = 24;

		private const int CommonSize = 34;

		public const int SizeToClient = 36;

		[Flags]
		public enum ClientFlags : byte
		{
			None = 0,
			SuppressRaResponses = 1,
			SubscribeServerConsole = 2,
			SubscribeServerLogs = 4,
			RemoteAdminMetadata = 8,
			RestrictPermissions = 16,
			SpecifyLogUsername = 32
		}
	}
}
