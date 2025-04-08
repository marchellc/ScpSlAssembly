using System;
using PlayerRoles;
using UnityEngine.Rendering;
using Utils.NonAllocLINQ;

namespace CustomPlayerEffects
{
	public class Traumatized : StatusEffectBase
	{
		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		protected override void Start()
		{
			base.Start();
			PlayerRoleManager.OnServerRoleSet += this.OnServerRoleChanged;
		}

		private void OnDestroy()
		{
			PlayerRoleManager.OnServerRoleSet -= this.OnServerRoleChanged;
		}

		private void OnServerRoleChanged(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (reason != RoleChangeReason.Died || newRole != RoleTypeId.Spectator || hub.GetRoleId() != RoleTypeId.Scp106)
			{
				return;
			}
			if (ReferenceHub.AllHubs.Any((ReferenceHub x) => x != hub && x.GetRoleId() == RoleTypeId.Scp106))
			{
				return;
			}
			base.ServerSetState(0, 0f, false);
		}

		protected override void Enabled()
		{
			base.Enabled();
			if (ReferenceHub.AllHubs.Any((ReferenceHub x) => x.GetRoleId() == RoleTypeId.Scp106))
			{
				return;
			}
			base.ServerSetState(0, 0f, false);
		}

		public Volume PPVolume;
	}
}
