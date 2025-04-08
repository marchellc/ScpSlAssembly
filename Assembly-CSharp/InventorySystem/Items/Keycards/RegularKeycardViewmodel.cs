using System;
using System.Diagnostics;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.SwayControllers;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Keycards
{
	public class RegularKeycardViewmodel : AnimatedViewmodelBase
	{
		public override IItemSwayController SwayController
		{
			get
			{
				return this._goopSway;
			}
		}

		public override float ViewmodelCameraFOV
		{
			get
			{
				return 40f;
			}
		}

		public override void InitLocal(ItemBase ib)
		{
			base.InitLocal(ib);
			InteractionCoordinator.OnClientInteracted += this.OnInteracted;
			this.SetupKeycard(ib.ItemTypeId);
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			this.SetupKeycard(id.TypeId);
			if (!this._spectatorEventSet)
			{
				KeycardItem.OnKeycardUsed += this.SpectatorOnKeycardUsed;
				this._spectatorEventSet = true;
			}
			if (wasEquipped)
			{
				this.AnimatorForceUpdate(base.SkipEquipTime, true);
				base.GetComponent<AudioSource>().mute = true;
			}
		}

		public override void InitAny()
		{
			base.InitAny();
			this._goopSway = new GoopSway(new GoopSway.GoopSwaySettings(this._swayPivot, 0.65f, 0.0035f, 0.04f, 7f, 6.5f, 0.03f, 1.6f, false), base.Hub);
		}

		private void SetupKeycard(ItemType keycardType)
		{
			KeycardItem keycardItem;
			if (!InventoryItemLoader.TryGetItem<KeycardItem>(keycardType, out keycardItem))
			{
				return;
			}
			KeycardPickup keycardPickup = keycardItem.PickupDropModel as KeycardPickup;
			if (keycardPickup == null)
			{
				return;
			}
			this._meshRenderer.sharedMaterial = keycardPickup.GetMaterialFromKeycardId(keycardType);
		}

		private void OnDestroy()
		{
			InteractionCoordinator.OnClientInteracted -= this.OnInteracted;
			if (this._spectatorEventSet)
			{
				KeycardItem.OnKeycardUsed -= this.SpectatorOnKeycardUsed;
			}
		}

		private void SpectatorOnKeycardUsed(ushort serial)
		{
			if (base.ItemId.SerialNumber != serial)
			{
				return;
			}
			this.PlayInteractAnimations();
		}

		private void OnInteracted(InteractableCollider col)
		{
			if (this == null)
			{
				return;
			}
			if (!base.gameObject.activeInHierarchy)
			{
				return;
			}
			if (this._stopwatch.IsRunning && this._stopwatch.Elapsed.TotalSeconds < 0.5)
			{
				return;
			}
			if (this.AnimatorStateInfo(0).shortNameHash == RegularKeycardViewmodel.AnimHash)
			{
				return;
			}
			DoorVariant doorVariant = col.Target as DoorVariant;
			if (doorVariant != null)
			{
				this.ProcessDoor(doorVariant, col.ColliderId);
				return;
			}
			Locker locker = col.Target as Locker;
			if (locker != null)
			{
				this.ProcessLocker(locker, (int)col.ColliderId);
			}
		}

		private void ProcessDoor(DoorVariant dv, byte colId)
		{
			if (dv.RequiredPermissions.RequiredPermissions == KeycardPermissions.None)
			{
				return;
			}
			if ((dv.GetExactState() > 0f && dv.GetExactState() < 1f) || !dv.AllowInteracting(ReferenceHub.LocalHub, colId))
			{
				return;
			}
			this.PlayInteractAnimations();
		}

		private void ProcessLocker(Locker locker, int chamberId)
		{
			if (chamberId >= locker.Chambers.Length)
			{
				return;
			}
			if (locker.Chambers[chamberId].RequiredPermissions == KeycardPermissions.None)
			{
				return;
			}
			if (!locker.Chambers[chamberId].AnimatorSet)
			{
				return;
			}
			this.PlayInteractAnimations();
		}

		protected virtual void PlayInteractAnimations()
		{
			this._stopwatch.Restart();
			this.AnimatorSetTrigger(RegularKeycardViewmodel.AnimHash);
			if (base.IsLocal)
			{
				NetworkClient.Send<KeycardItem.UseMessage>(new KeycardItem.UseMessage(base.ItemId.SerialNumber), 0);
			}
		}

		private static readonly int AnimHash = Animator.StringToHash("Use");

		[SerializeField]
		private MeshRenderer _meshRenderer;

		[SerializeField]
		private Transform _swayPivot;

		private GoopSway _goopSway;

		private bool _spectatorEventSet;

		private const float UseCooldown = 0.5f;

		private readonly Stopwatch _stopwatch = new Stopwatch();
	}
}
