using System;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class ShootingParticlesExtension : ShootingEffectsExtensionBase
	{
		protected override void Awake()
		{
			base.Awake();
			foreach (ShootingEffectsExtensionBase.ParticleCollection particleCollection in this._systemsPerBarrel)
			{
				particleCollection.ConvertLights();
			}
		}

		protected override void PlayEffects(ShotEvent ev)
		{
			IMultiBarreledShot multiBarreledShot = ev as IMultiBarreledShot;
			if (multiBarreledShot == null)
			{
				return;
			}
			ShootingEffectsExtensionBase.ParticleCollection particleCollection;
			if (!this._systemsPerBarrel.TryGet(multiBarreledShot.BarrelId, out particleCollection))
			{
				return;
			}
			particleCollection.Play();
		}

		[SerializeField]
		private ShootingEffectsExtensionBase.ParticleCollection[] _systemsPerBarrel;
	}
}
