using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Subroutines;

public abstract class SingleTargetAttackAbility<T> : ScpAttackAbilityBase<T> where T : PlayerRoleBase, IFpcRole
{
	protected override void DamagePlayers()
	{
		Transform playerCameraReference = base.Owner.PlayerCameraReference;
		ReferenceHub primaryTarget = base.DetectedPlayers.GetPrimaryTarget(playerCameraReference);
		if (!(primaryTarget == null))
		{
			this.DamagePlayer(primaryTarget, this.DamageAmount);
		}
	}
}
