using System;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class Scp2536Projectile : FlybyDetectorProjectile
	{
		public override void ServerProcessHit(HitboxIdentity hid)
		{
			base.ServerProcessHit(hid);
			ReferenceHub targetHub = hid.TargetHub;
			HumanRole humanRole = targetHub.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			if (!HitboxIdentity.IsDamageable(this.PreviousOwner.Role, humanRole.RoleTypeId))
			{
				return;
			}
			FpcStandardScp fpcStandardScp = null;
			Vector3 position = humanRole.FpcModule.Position;
			float num = float.MaxValue;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				FpcStandardScp fpcStandardScp2 = referenceHub.roleManager.CurrentRole as FpcStandardScp;
				if (fpcStandardScp2 != null)
				{
					float sqrMagnitude = (fpcStandardScp2.FpcModule.Position - position).sqrMagnitude;
					if (sqrMagnitude <= num)
					{
						fpcStandardScp = fpcStandardScp2;
						num = sqrMagnitude;
					}
				}
			}
			if (fpcStandardScp == null)
			{
				return;
			}
			targetHub.TryOverridePosition(fpcStandardScp.FpcModule.Position);
			targetHub.TryOverrideRotation(Vector3.zero);
		}

		public override bool Weaved()
		{
			return true;
		}
	}
}
