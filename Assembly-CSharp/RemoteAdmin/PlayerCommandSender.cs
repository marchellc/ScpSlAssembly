namespace RemoteAdmin;

public class PlayerCommandSender : CommandSender
{
	public readonly ReferenceHub ReferenceHub;

	public override string SenderId => this.ReferenceHub.authManager.UserId;

	public int PlayerId => this.ReferenceHub.PlayerId;

	public override string Nickname => this.ReferenceHub.nicknameSync.MyNick;

	public override ulong Permissions => this.ReferenceHub.serverRoles.Permissions;

	public override byte KickPower
	{
		get
		{
			if (!this.ReferenceHub.authManager.RemoteAdminGlobalAccess)
			{
				return this.ReferenceHub.serverRoles.KickPower;
			}
			return byte.MaxValue;
		}
	}

	public override bool FullPermissions => false;

	public override string LogName => this.Nickname + " (" + this.ReferenceHub.authManager.UserId + ")";

	public PlayerCommandSender(ReferenceHub hub)
	{
		this.ReferenceHub = hub;
	}

	public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
	{
		this.ReferenceHub.queryProcessor.SendToClient(text, success, logToConsole, overrideDisplay);
	}

	public override void Print(string text)
	{
		this.ReferenceHub.queryProcessor.SendToClient(text, isSuccess: true, logInConsole: true, "");
	}

	public override bool Available()
	{
		return true;
	}
}
