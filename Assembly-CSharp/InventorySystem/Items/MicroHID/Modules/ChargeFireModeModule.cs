using System;
using Footprinting;
using Interactables;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class ChargeFireModeModule : FiringModeControllerModule
	{
		public override MicroHidFiringMode AssignedMode
		{
			get
			{
				return MicroHidFiringMode.ChargeFire;
			}
		}

		public override float WindUpRate
		{
			get
			{
				return 0.14285715f;
			}
		}

		public override float WindDownRate
		{
			get
			{
				return 0.2f;
			}
		}

		public override float DrainRateWindUp
		{
			get
			{
				return 0.01f;
			}
		}

		public override float DrainRateSustain
		{
			get
			{
				return 0.005f;
			}
		}

		public override float DrainRateFiring
		{
			get
			{
				return 0.1f;
			}
		}

		public override bool ValidateStart
		{
			get
			{
				return base.InputSync.Secondary && !base.Broken;
			}
		}

		public override bool ValidateEnterFire
		{
			get
			{
				return base.InputSync.Primary;
			}
		}

		public override bool ValidateUpdate
		{
			get
			{
				return (base.InputSync.Primary || base.InputSync.Secondary) && base.Energy > 0f && !base.Broken;
			}
		}

		public override float FiringRange
		{
			get
			{
				return 10f;
			}
		}

		public override float BacktrackerDot
		{
			get
			{
				return 0.9f;
			}
		}

		public override void ServerUpdateSelected(MicroHidPhase status)
		{
			base.ServerUpdateSelected(status);
			if (status != this._prevStatus)
			{
				this.ServerOnModeChanged(status);
				this._prevStatus = status;
			}
			if (status == MicroHidPhase.WoundUpSustain)
			{
				this.ServerUpdateWoundUp();
				return;
			}
			if (status != MicroHidPhase.Firing)
			{
				return;
			}
			base.ServerRequestBacktrack(new Action(this.ServerFire));
		}

		private void ServerOnModeChanged(MicroHidPhase status)
		{
			if (status != MicroHidPhase.WoundUpSustain)
			{
				if (status == MicroHidPhase.Firing)
				{
					base.Energy -= 0.1f;
					return;
				}
			}
			else
			{
				this._woundUpElapsed = 0f;
			}
		}

		private void ServerUpdateWoundUp()
		{
			this._woundUpElapsed += Time.deltaTime;
			if (this._woundUpElapsed < 15f)
			{
				return;
			}
			this.ServerExplode();
		}

		private void ServerExplode()
		{
			if (this._alreadyExploded)
			{
				return;
			}
			this._alreadyExploded = true;
			base.MicroHid.BrokenSync.ServerSetBroken();
			ReferenceHub owner = base.Item.Owner;
			IFpcRole fpcRole = owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			base.Energy -= 0.25f;
			Vector3 position = fpcRole.FpcModule.Position;
			int num;
			HitregUtils.OverlapSphere(position, 10f, out num, null);
			for (int i = 0; i < num; i++)
			{
				InteractableCollider interactableCollider;
				if (HitregUtils.DetectionsNonAlloc[i].TryGetComponent<InteractableCollider>(out interactableCollider))
				{
					IDamageableDoor damageableDoor = interactableCollider.Target as IDamageableDoor;
					if (damageableDoor != null && this.CheckIntercolLineOfSight(position, interactableCollider))
					{
						damageableDoor.ServerDamage(350f, DoorDamageType.Grenade, new Footprint(owner));
					}
				}
			}
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(position, true);
			foreach (RoomLightController roomLightController in RoomLightController.Instances)
			{
				if (!(roomLightController.Room != roomIdentifier) && roomLightController.LightsEnabled)
				{
					roomLightController.ServerFlickerLights(1f);
				}
			}
			owner.playerStats.DealDamage(new MicroHidDamageHandler(125f, base.MicroHid));
		}

		private void ServerFire()
		{
			Transform playerCameraReference = base.Item.Owner.PlayerCameraReference;
			int num;
			HitregUtils.Raycast(playerCameraReference, 0.65f, this.FiringRange, out num);
			foreach (IDestructible destructible in HitregUtils.DetectedDestructibles)
			{
				destructible.ServerDealDamage(this, 1000f * Time.deltaTime);
			}
			for (int i = 0; i < num; i++)
			{
				InteractableCollider interactableCollider;
				if (HitregUtils.DetectionsNonAlloc[i].TryGetComponent<InteractableCollider>(out interactableCollider) && this.CheckIntercolLineOfSight(playerCameraReference.position, interactableCollider))
				{
					this.HandlePotentialDoor(interactableCollider);
				}
			}
		}

		private void HandlePotentialDoor(InteractableCollider interactable)
		{
			BreakableDoor breakableDoor = interactable.Target as BreakableDoor;
			if (breakableDoor == null)
			{
				return;
			}
			if (breakableDoor.TargetState)
			{
				return;
			}
			if (!breakableDoor.AllowInteracting(base.Item.Owner, interactable.ColliderId))
			{
				return;
			}
			if ((breakableDoor.ActiveLocks & (ushort)(~(ushort)ChargeFireModeModule.BypassableLocks)) == 0)
			{
				breakableDoor.NetworkTargetState = true;
			}
		}

		private bool CheckIntercolLineOfSight(Vector3 originPoint, InteractableCollider collider)
		{
			Transform transform = collider.transform;
			Vector3 vector = transform.position + transform.TransformDirection(collider.VerificationOffset);
			RaycastHit raycastHit;
			return !Physics.Linecast(originPoint, vector, out raycastHit, PlayerRolesUtils.BlockerMask) || raycastHit.collider.transform == transform;
		}

		private static readonly DoorLockReason BypassableLocks = DoorLockReason.Regular079 | DoorLockReason.Lockdown079 | DoorLockReason.NoPower | DoorLockReason.Lockdown2176;

		public const float MaxSustainBeforeExplosionSeconds = 15f;

		private const float RaycastThickness = 0.65f;

		private const float DamagePerSec = 1000f;

		private const float StartFiringEnergyUse = 0.1f;

		private const float ExplosionEnergyPenalty = 0.25f;

		private const float ExplosionPlayerDamage = 125f;

		private const float ExplosionDoorDamage = 350f;

		private const float ExplosionRadius = 10f;

		private const float ExplosionBlackoutSeconds = 1f;

		private MicroHidPhase _prevStatus;

		private float _woundUpElapsed;

		private bool _alreadyExploded;
	}
}
