using System;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class ThrownProjectile : CollisionDetectionPickup
	{
		public static event Action<ThrownProjectile> OnProjectileSpawned;

		protected override void Start()
		{
			base.Start();
			Action<ThrownProjectile> onProjectileSpawned = ThrownProjectile.OnProjectileSpawned;
			if (onProjectileSpawned == null)
			{
				return;
			}
			onProjectileSpawned(this);
		}

		public virtual void ToggleRenderers(bool state)
		{
			this._renderersRoot.SetActive(state);
		}

		public virtual void ServerOnThrown(Vector3 torque, Vector3 velocity)
		{
		}

		public virtual void ServerActivate()
		{
		}

		// Note: this type is marked as 'beforefieldinit'.
		static ThrownProjectile()
		{
			ThrownProjectile.OnProjectileSpawned = delegate(ThrownProjectile thrownProjectile)
			{
			};
			ThrownProjectile.HitBlockerMask = new CachedLayerMask(new string[] { "Default", "Glass", "CCTV", "Door" });
		}

		public override bool Weaved()
		{
			return true;
		}

		public static readonly CachedLayerMask HitBlockerMask;

		[SerializeField]
		private GameObject _renderersRoot;
	}
}
