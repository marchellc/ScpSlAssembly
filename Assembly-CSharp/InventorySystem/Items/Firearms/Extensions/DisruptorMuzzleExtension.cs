using System;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class DisruptorMuzzleExtension : ShootingEffectsExtensionBase
	{
		protected override void PlayEffects(ShotEvent ev)
		{
			DisruptorShotEvent disruptorShotEvent = ev as DisruptorShotEvent;
			if (disruptorShotEvent == null)
			{
				return;
			}
			DisruptorActionModule.FiringState state = disruptorShotEvent.State;
			if (state != DisruptorActionModule.FiringState.FiringRapid)
			{
				if (state == DisruptorActionModule.FiringState.FiringSingle)
				{
					this._singleShotEffects.Play();
					return;
				}
			}
			else
			{
				this._rapidFireEffects.Play();
			}
		}

		[SerializeField]
		private ShootingEffectsExtensionBase.ParticleCollection _singleShotEffects;

		[SerializeField]
		private ShootingEffectsExtensionBase.ParticleCollection _rapidFireEffects;
	}
}
