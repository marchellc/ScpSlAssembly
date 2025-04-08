using System;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Keycards
{
	public class KeycardPickup : CollisionDetectionPickup
	{
		public Material GetMaterialFromKeycardId(ItemType itemId)
		{
			return this._keycardTextures[Mathf.Min(this._keycardTextures.Length - 1, (int)itemId)];
		}

		protected override void Start()
		{
			base.Start();
			this.UpdateMaterial();
			base.OnInfoChanged += this.UpdateMaterial;
		}

		private void UpdateMaterial()
		{
			this._targetRenderer.sharedMaterial = this.GetMaterialFromKeycardId(this.Info.ItemId);
		}

		protected override void ProcessCollision(Collision collision)
		{
			base.ProcessCollision(collision);
			if (!NetworkServer.active)
			{
				return;
			}
			RegularDoorButton regularDoorButton;
			if (!collision.collider.TryGetComponent<RegularDoorButton>(out regularDoorButton))
			{
				return;
			}
			DoorVariant doorVariant = regularDoorButton.Target as DoorVariant;
			if (doorVariant == null || doorVariant.ActiveLocks != 0 || doorVariant.RequiredPermissions.RequiredPermissions == KeycardPermissions.None)
			{
				return;
			}
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(this.Info.ItemId, out itemBase) || !doorVariant.RequiredPermissions.CheckPermissions(itemBase, null))
			{
				return;
			}
			if (doorVariant.AllowInteracting(null, regularDoorButton.ColliderId))
			{
				doorVariant.NetworkTargetState = !doorVariant.TargetState;
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		[SerializeField]
		private Material[] _keycardTextures;

		[SerializeField]
		private MeshRenderer _targetRenderer;
	}
}
