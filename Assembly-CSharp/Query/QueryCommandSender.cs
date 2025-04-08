using System;

namespace Query
{
	public class QueryCommandSender : CommandSender
	{
		internal QueryCommandSender(QueryUser usr)
		{
			this._qu = usr;
		}

		public override string SenderId
		{
			get
			{
				return this._qu.SenderID;
			}
		}

		public override string Nickname
		{
			get
			{
				return this._qu.SenderID;
			}
		}

		public override ulong Permissions
		{
			get
			{
				return this._qu.QueryPermissions;
			}
		}

		public override byte KickPower
		{
			get
			{
				return this._qu.QueryKickPower;
			}
		}

		public override bool FullPermissions
		{
			get
			{
				return this._qu.QueryPermissions == ulong.MaxValue && this.KickPower == byte.MaxValue;
			}
		}

		public override void Print(string text)
		{
			this._qu.Send(text, QueryMessage.ClientReceivedContentType.ConsoleString);
		}

		public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
		{
			if (this._qu.ClientFlags.HasFlagFast(QueryHandshake.ClientFlags.SuppressRaResponses))
			{
				return;
			}
			if (this._qu.ClientFlags.HasFlagFast(QueryHandshake.ClientFlags.RemoteAdminMetadata))
			{
				this._qu.Send(new RemoteAdminResponse(text, success, logToConsole, overrideDisplay).Serialize(), QueryMessage.ClientReceivedContentType.RemoteAdminSerializedResponse);
				return;
			}
			if (success)
			{
				this._qu.Send(text, QueryMessage.ClientReceivedContentType.RemoteAdminPlaintextResponse);
				return;
			}
			this._qu.Send(text, QueryMessage.ClientReceivedContentType.RemoteAdminUnsuccessfulPlaintextResponse);
		}

		public override bool Available()
		{
			return this._qu.Connected;
		}

		public void Disconnect()
		{
			this._qu.Disconnect();
		}

		private readonly QueryUser _qu;
	}
}
