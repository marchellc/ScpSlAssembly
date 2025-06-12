namespace Query;

public class QueryCommandSender : CommandSender
{
	private readonly QueryUser _qu;

	public override string SenderId => this._qu.SenderID;

	public override string Nickname => this._qu.SenderID;

	public override ulong Permissions => this._qu.QueryPermissions;

	public override byte KickPower => this._qu.QueryKickPower;

	public override bool FullPermissions
	{
		get
		{
			if (this._qu.QueryPermissions == ulong.MaxValue)
			{
				return this.KickPower == byte.MaxValue;
			}
			return false;
		}
	}

	internal QueryCommandSender(QueryUser usr)
	{
		this._qu = usr;
	}

	public override void Print(string text)
	{
		this._qu.Send(text, QueryMessage.ClientReceivedContentType.ConsoleString);
	}

	public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
	{
		if (!this._qu.ClientFlags.HasFlagFast(QueryHandshake.ClientFlags.SuppressRaResponses))
		{
			if (this._qu.ClientFlags.HasFlagFast(QueryHandshake.ClientFlags.RemoteAdminMetadata))
			{
				this._qu.Send(new RemoteAdminResponse(text, success, logToConsole, overrideDisplay).Serialize(), QueryMessage.ClientReceivedContentType.RemoteAdminSerializedResponse);
			}
			else if (success)
			{
				this._qu.Send(text, QueryMessage.ClientReceivedContentType.RemoteAdminPlaintextResponse);
			}
			else
			{
				this._qu.Send(text, QueryMessage.ClientReceivedContentType.RemoteAdminUnsuccessfulPlaintextResponse);
			}
		}
	}

	public override bool Available()
	{
		return this._qu.Connected;
	}

	public void Disconnect()
	{
		this._qu.Disconnect();
	}
}
