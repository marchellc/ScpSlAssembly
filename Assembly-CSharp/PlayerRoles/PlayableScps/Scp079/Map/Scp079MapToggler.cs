using System;
using Mirror;
using PlayerRoles.Spectating;
using ToggleableMenus;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class Scp079MapToggler : Scp079ToggleMenuAbilityBase<Scp079MapToggler>, IRegisterableMenu
	{
		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Inventory;
			}
		}

		protected override Scp079HudTranslation OpenTranslation
		{
			get
			{
				return Scp079HudTranslation.OpenMap;
			}
		}

		protected override Scp079HudTranslation CloseTranslation
		{
			get
			{
				return Scp079HudTranslation.CloseMap;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return false;
			}
		}

		public bool IsEnabled
		{
			get
			{
				return Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen;
			}
			set
			{
				if (!base.Role.IsLocalPlayer)
				{
					return;
				}
				if (value == Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen)
				{
					return;
				}
				this.Trigger();
			}
		}

		private void LateUpdate()
		{
			if (!base.Role.IsLocalPlayer || !base.SyncState)
			{
				return;
			}
			base.ClientSendCmd();
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			if (!base.SyncState)
			{
				return;
			}
			writer.WriteVector3(Scp079MapGui.SyncVars);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			if (reader.Remaining > 0)
			{
				base.SyncState = true;
				Scp079MapGui.SyncVars = reader.ReadVector3();
				base.ServerSendRpc((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole);
				return;
			}
			base.SyncState = false;
			base.ServerSendRpc((ReferenceHub x) => x != base.Owner);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			if (base.SyncState)
			{
				writer.WriteVector3(Scp079MapGui.SyncVars);
			}
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (base.SyncState)
			{
				Scp079MapGui.SyncVars = reader.ReadVector3();
			}
		}
	}
}
