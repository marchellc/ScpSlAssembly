using System;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class MultiBarrelHitscan : HitscanHitregModuleBase
{
	[Serializable]
	private struct BarrelOffset
	{
		public float RightPosition;

		public float TopPosition;

		public float RightDirection;

		public float TopDirection;
	}

	private Ray? _lastRay;

	[SerializeField]
	private BarrelOffset[] _barrels;

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		this._lastRay = null;
	}

	protected override float HitmarkerSizeAtDamage(float damage)
	{
		return Mathf.Clamp01(base.HitmarkerSizeAtDamage(damage));
	}

	protected override void Fire()
	{
		Ray valueOrDefault = this._lastRay.GetValueOrDefault();
		if (!this._lastRay.HasValue)
		{
			valueOrDefault = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
			this._lastRay = valueOrDefault;
		}
		if (!(base.LastShotEvent is BulletShotEvent bulletShotEvent) || !this._barrels.TryGet(bulletShotEvent.BarrelId, out var element))
		{
			this.ServerApplyDamage(base.ServerPrescan(this._lastRay.Value));
		}
		else
		{
			this.ShootOffset(element);
		}
	}

	private void ShootOffset(BarrelOffset offset)
	{
		Transform playerCameraReference = base.Firearm.Owner.PlayerCameraReference;
		Vector3 origin = this._lastRay.Value.origin;
		origin += playerCameraReference.up * offset.TopPosition;
		origin += playerCameraReference.right * offset.RightPosition;
		Vector3 direction = this._lastRay.Value.direction;
		direction += Vector3.up * offset.TopDirection;
		Ray targetRay = new Ray(origin, (direction + Vector3.right * offset.RightDirection).normalized);
		this.ServerApplyDamage(base.ServerPrescan(targetRay));
	}
}
