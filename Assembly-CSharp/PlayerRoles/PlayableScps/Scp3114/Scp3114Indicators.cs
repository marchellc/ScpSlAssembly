using System;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.Ragdolls;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Indicators : RagdollIndicatorsBase<Scp3114Role>
	{
		protected override bool ValidateRagdoll(BasicRagdoll ragdoll)
		{
			if (!base.CastRole.Disguised && ragdoll.Info.RoleType.IsHuman())
			{
				Scp3114DamageHandler scp3114DamageHandler = ragdoll.Info.Handler as Scp3114DamageHandler;
				return scp3114DamageHandler == null || scp3114DamageHandler.Subtype != Scp3114DamageHandler.HandlerType.SkinSteal;
			}
			return false;
		}
	}
}
