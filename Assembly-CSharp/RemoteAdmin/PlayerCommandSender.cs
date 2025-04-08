using System;

namespace RemoteAdmin
{
	public class PlayerCommandSender : CommandSender
	{
		public PlayerCommandSender(ReferenceHub hub)
		{
			this.ReferenceHub = hub;
		}

		public override string SenderId
		{
			get
			{
				return this.ReferenceHub.authManager.UserId;
			}
		}

		public int PlayerId
		{
			get
			{
				return this.ReferenceHub.PlayerId;
			}
		}

		public override string Nickname
		{
			get
			{
				return this.ReferenceHub.nicknameSync.MyNick;
			}
		}

		public override ulong Permissions
		{
			get
			{
				return this.ReferenceHub.serverRoles.Permissions;
			}
		}

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

		public override bool FullPermissions
		{
			get
			{
				return false;
			}
		}

		public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
		{
			this.ReferenceHub.queryProcessor.SendToClient(text, success, logToConsole, overrideDisplay);
		}

		public override void Print(string text)
		{
			this.ReferenceHub.queryProcessor.SendToClient(text, true, true, "");
		}

		public override bool Available()
		{
			return true;
		}

		public override string LogName
		{
			get
			{
				return this.Nickname + " (" + this.ReferenceHub.authManager.UserId + ")";
			}
		}

		public readonly ReferenceHub ReferenceHub;
	}
}
