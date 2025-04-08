using System;
using PlayerRoles;
using UnityEngine;

namespace Respawning.Waves
{
	public class ChaosMiniWaveAnimation : WaveAnimationBase<ChaosMiniWave>
	{
		protected override void OnAnimationEnd()
		{
			base.OnAnimationEnd();
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			Team team = referenceHub.GetTeam();
			if (team != Team.Dead && team.GetFaction() != Faction.FoundationEnemy)
			{
				return;
			}
			this._announcement.Play();
		}

		[SerializeField]
		private AudioSource _announcement;
	}
}
