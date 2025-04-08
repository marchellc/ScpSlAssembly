using System;
using PlayerRoles.Ragdolls;

namespace PlayerRoles.PlayableScps.Scp049
{
	public class Scp049ResurrectIndicators : RagdollIndicatorsBase<Scp049Role>
	{
		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp049ResurrectAbility>(out this._resurrectAbility);
		}

		protected override bool ValidateRagdoll(BasicRagdoll ragdoll)
		{
			return this._resurrectAbility.CheckRagdoll(ragdoll);
		}

		private Scp049ResurrectAbility _resurrectAbility;
	}
}
