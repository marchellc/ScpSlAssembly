using System;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507Indicators : RagdollIndicatorsBase<Scp1507Role>
	{
		protected override GameObject GenerateIndicator(BasicRagdoll ragdoll)
		{
			GameObject gameObject = base.GenerateIndicator(ragdoll);
			Scp1507CorpseIndicator scp1507CorpseIndicator;
			if (gameObject.TryGetComponent<Scp1507CorpseIndicator>(out scp1507CorpseIndicator))
			{
				scp1507CorpseIndicator.Ragdoll = ragdoll as Scp1507Ragdoll;
			}
			return gameObject;
		}

		protected override bool ValidateRagdoll(BasicRagdoll basicRagdoll)
		{
			Scp1507Ragdoll scp1507Ragdoll = basicRagdoll as Scp1507Ragdoll;
			return scp1507Ragdoll != null && scp1507Ragdoll.ValidateRevive(base.Owner);
		}
	}
}
