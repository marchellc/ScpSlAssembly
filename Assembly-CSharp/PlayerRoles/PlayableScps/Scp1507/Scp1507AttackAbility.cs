using System;
using CameraShaking;
using Footprinting;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507AttackAbility : SingleTargetAttackAbility<Scp1507Role>, IShakeEffect
	{
		public event Action ServerOnDoorAttacked;

		public event Action ServerOnMissed;

		public override float DamageAmount
		{
			get
			{
				return this._damage;
			}
		}

		protected override float AttackDelay
		{
			get
			{
				return 0f;
			}
		}

		protected override float BaseCooldown
		{
			get
			{
				return 0.6f;
			}
		}

		protected override bool SelfRepeating
		{
			get
			{
				return false;
			}
		}

		private bool TryAttackDoor()
		{
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			RaycastHit raycastHit;
			if (!Physics.Raycast(playerCameraReference.position, playerCameraReference.forward, out raycastHit, 1.728f, Scp1507AttackAbility.AttackMask))
			{
				return false;
			}
			InteractableCollider interactableCollider;
			if (!raycastHit.collider.TryGetComponent<InteractableCollider>(out interactableCollider))
			{
				return false;
			}
			DoorVariant doorVariant = interactableCollider.Target as DoorVariant;
			if (doorVariant == null || doorVariant.TargetState)
			{
				return false;
			}
			IDamageableDoor damageableDoor = interactableCollider.Target as IDamageableDoor;
			if (damageableDoor != null && damageableDoor.ServerDamage(15f, DoorDamageType.Scp096, default(Footprint)))
			{
				Hitmarker.SendHitmarkerDirectly(base.Owner, 15f, true);
				return true;
			}
			if (doorVariant.AllowInteracting(base.Owner, interactableCollider.ColliderId) && DoorLockUtils.GetMode(doorVariant).HasFlagFast(DoorLockMode.CanOpen))
			{
				doorVariant.NetworkTargetState = true;
				return true;
			}
			return false;
		}

		protected override DamageHandlerBase DamageHandler(float damage)
		{
			return new Scp1507DamageHandler(new Footprint(base.Owner), damage);
		}

		protected override void DamagePlayer(ReferenceHub hub, float damage)
		{
			if (hub.IsSCP(true))
			{
				damage *= 2f;
			}
			base.DamagePlayer(hub, damage);
		}

		protected override void DamagePlayers()
		{
			base.DamagePlayers();
			if (base.LastAttackResult != AttackResult.None)
			{
				return;
			}
			if (this.TryAttackDoor())
			{
				Action serverOnDoorAttacked = this.ServerOnDoorAttacked;
				if (serverOnDoorAttacked == null)
				{
					return;
				}
				serverOnDoorAttacked();
				return;
			}
			else
			{
				Action serverOnMissed = this.ServerOnMissed;
				if (serverOnMissed == null)
				{
					return;
				}
				serverOnMissed();
				return;
			}
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			CameraShakeController.AddEffect(this);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._attackRandom = global::UnityEngine.Random.Range(-1f, 1f);
		}

		public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
		{
			shakeValues = ShakeEffectValues.None;
			if (base.Role.Pooled)
			{
				return false;
			}
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return true;
			}
			float num = base.Cooldown.Readiness * this._peckShakeTimeScale;
			float num2 = this._peckShakeFov.Evaluate(num);
			float num3 = this._attackRandom * this._peckShakeAngle.Evaluate(num);
			Quaternion quaternion = Quaternion.Euler(0f, 0f, num3);
			Quaternion? quaternion2 = new Quaternion?(quaternion);
			float num4 = num2;
			shakeValues = new ShakeEffectValues(quaternion2, null, null, num4, 0f, 0f);
			return true;
		}

		private const float ScpDamageMultiplier = 2f;

		private const float AttackDistance = 1.728f;

		private const float DoorDamageAmount = 15f;

		public static readonly CachedLayerMask AttackMask = new CachedLayerMask(new string[] { "Default", "InteractableNoPlayerCollision", "Glass", "Door" });

		[SerializeField]
		private float _damage;

		[SerializeField]
		private AnimationCurve _peckShakeAngle;

		[SerializeField]
		private AnimationCurve _peckShakeFov;

		[SerializeField]
		private float _peckShakeTimeScale;

		private float _attackRandom;
	}
}
