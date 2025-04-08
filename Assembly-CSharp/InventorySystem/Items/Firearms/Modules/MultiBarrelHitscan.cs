using System;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class MultiBarrelHitscan : HitscanHitregModuleBase
	{
		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			this._lastRay = null;
		}

		protected override void Fire()
		{
			Ray ray = this._lastRay.GetValueOrDefault();
			if (this._lastRay == null)
			{
				ray = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
				this._lastRay = new Ray?(ray);
			}
			BulletShotEvent bulletShotEvent = base.LastShotEvent as BulletShotEvent;
			MultiBarrelHitscan.BarrelOffset barrelOffset;
			float num;
			if (bulletShotEvent == null || !this._barrels.TryGet(bulletShotEvent.BarrelId, out barrelOffset))
			{
				base.ServerPerformHitscan(this._lastRay.Value, out num);
			}
			else
			{
				this.ShootOffset(barrelOffset, out num);
			}
			this.SendHitmarker(num);
		}

		private void ShootOffset(MultiBarrelHitscan.BarrelOffset offset, out float dmgDealt)
		{
			Transform playerCameraReference = base.Firearm.Owner.PlayerCameraReference;
			Vector3 vector = this._lastRay.Value.origin;
			vector += playerCameraReference.up * offset.TopPosition;
			vector += playerCameraReference.right * offset.RightPosition;
			Vector3 vector2 = this._lastRay.Value.direction;
			vector2 += Vector3.up * offset.TopDirection;
			base.ServerPerformHitscan(new Ray(vector, (vector2 + Vector3.right * offset.RightDirection).normalized), out dmgDealt);
		}

		private Ray? _lastRay;

		[SerializeField]
		private MultiBarrelHitscan.BarrelOffset[] _barrels;

		[Serializable]
		private struct BarrelOffset
		{
			public float RightPosition;

			public float TopPosition;

			public float RightDirection;

			public float TopDirection;
		}
	}
}
