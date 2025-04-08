using System;
using Mirror;
using PlayerRoles;

namespace Metrics
{
	public class RoleChangeCollector : MetricsCollectorBase
	{
		public override void Init()
		{
			base.Init();
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.RecordData<RoleChangeCollector>(new RoleChangeCollector
			{
				PrevLifeId = prevRole.UniqueLifeIdentifier,
				NewLifeId = newRole.UniqueLifeIdentifier,
				NewRole = newRole.RoleTypeId,
				ChangeReason = newRole.ServerSpawnReason
			}, true);
		}

		public int PrevLifeId;

		public int NewLifeId;

		public RoleTypeId NewRole;

		public RoleChangeReason ChangeReason;
	}
}
