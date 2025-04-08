using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Armor
{
	public class BodyArmorPickup : ItemPickupBase
	{
		private bool IsAffected
		{
			get
			{
				return !this._released && NetworkServer.active && this.PreviousOwner.IsSet;
			}
		}

		protected override void Start()
		{
			base.Start();
			if (!this.IsAffected || this.PreviousOwner.Hub == null)
			{
				return;
			}
			this._remainingReleaseTime = 0.15f;
			this._rb = (base.PhysicsModule as PickupStandardPhysics).Rb;
			this._rb.rotation = this.PreviousOwner.Hub.transform.rotation * BodyArmorPickup.StartRotation;
			this._rb.constraints = BodyArmorPickup.StartConstraints;
		}

		private void OnTriggerStay(Collider other)
		{
			if (other.gameObject.layer != 9)
			{
				return;
			}
			if (Vector3.Dot(Vector3.up, base.transform.right) > -0.8f)
			{
				return;
			}
			ItemPickupBase itemPickupBase;
			if (!other.transform.root.TryGetComponent<ItemPickupBase>(out itemPickupBase))
			{
				return;
			}
			if (itemPickupBase.Info.WeightKg > 2.1f || !this._alreadyMovedPickups.Add(itemPickupBase.Info.Serial))
			{
				return;
			}
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(itemPickupBase.Info.ItemId, out itemBase))
			{
				return;
			}
			if (itemBase.Category == ItemCategory.Armor)
			{
				return;
			}
			float num = base.transform.position.y - itemPickupBase.transform.position.y;
			itemPickupBase.transform.position += Vector3.up * (num * 2f + 0.16f);
		}

		private void Update()
		{
			if (!this.IsAffected)
			{
				return;
			}
			if (Mathf.Abs(this._rb.velocity.y) > 0.1f)
			{
				return;
			}
			this._remainingReleaseTime -= Time.deltaTime;
			if (this._remainingReleaseTime <= 0f)
			{
				this._released = true;
				this._rb.constraints = RigidbodyConstraints.None;
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		private static readonly RigidbodyConstraints StartConstraints = (RigidbodyConstraints)80;

		private static readonly Quaternion StartRotation = Quaternion.Euler(0f, 0f, -90f);

		private readonly HashSet<ushort> _alreadyMovedPickups = new HashSet<ushort>();

		private const float ReleaseVelocity = 0.1f;

		private const float ReleaseDelay = 0.15f;

		private const float DotProductThreshold = -0.8f;

		private const float HeightOffset = 0.16f;

		private const float WeightLimit = 2.1f;

		private const int PickupLayer = 9;

		private float _remainingReleaseTime;

		private bool _released;

		private Rigidbody _rb;
	}
}
