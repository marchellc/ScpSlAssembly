using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Footprinting;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class ExplosionGrenade : EffectGrenade
	{
		public static event Action<Footprint, Vector3, ExplosionGrenade> OnExploded;

		public override void PlayExplosionEffects(Vector3 pos)
		{
			base.PlayExplosionEffects(pos);
			if (!MainCameraController.InstanceActive)
			{
				return;
			}
			float num = Vector3.Distance(MainCameraController.CurrentCamera.position, pos);
			ExplosionCameraShake.singleton.Shake(this._shakeOverDistance.Evaluate(num));
			if (!NetworkServer.active)
			{
				return;
			}
			ExplosionGrenade.Explode(this.PreviousOwner, pos, this, ExplosionType.Grenade);
		}

		public override bool ServerFuseEnd()
		{
			if (!base.ServerFuseEnd())
			{
				return false;
			}
			ExplosionGrenade.Explode(this.PreviousOwner, base.transform.position, this, ExplosionType.Grenade);
			ServerEvents.OnProjectileExploded(new ProjectileExplodedEventArgs(this, this.PreviousOwner.Hub, base.transform.position));
			return true;
		}

		public static void Explode(Footprint attacker, Vector3 position, ExplosionGrenade settingsReference, ExplosionType explosionType)
		{
			bool flag = true;
			ExplosionSpawningEventArgs explosionSpawningEventArgs = new ExplosionSpawningEventArgs(attacker.Hub, position, settingsReference, explosionType, flag);
			ServerEvents.OnExplosionSpawning(explosionSpawningEventArgs);
			if (!explosionSpawningEventArgs.IsAllowed)
			{
				return;
			}
			position = explosionSpawningEventArgs.Position;
			settingsReference = explosionSpawningEventArgs.Settings;
			flag = explosionSpawningEventArgs.DestroyDoors;
			explosionType = explosionSpawningEventArgs.ExplosionType;
			ReferenceHub hub = attacker.Hub;
			Player player = explosionSpawningEventArgs.Player;
			if (hub != ((player != null) ? player.ReferenceHub : null))
			{
				Player player2 = explosionSpawningEventArgs.Player;
				attacker = new Footprint((player2 != null) ? player2.ReferenceHub : null);
			}
			ExplosionGrenade.SetHostHitboxes(true);
			HashSet<uint> hashSet = HashSetPool<uint>.Shared.Rent();
			HashSet<uint> hashSet2 = HashSetPool<uint>.Shared.Rent();
			float maxRadius = settingsReference.MaxRadius;
			foreach (Collider collider in Physics.OverlapSphere(position, maxRadius, settingsReference.DetectionMask))
			{
				if (NetworkServer.active)
				{
					IExplosionTrigger explosionTrigger;
					if (collider.TryGetComponent<IExplosionTrigger>(out explosionTrigger))
					{
						explosionTrigger.OnExplosionDetected(attacker, position, maxRadius);
					}
					IDestructible destructible;
					if (collider.TryGetComponent<IDestructible>(out destructible))
					{
						if (!hashSet.Contains(destructible.NetworkId) && ExplosionGrenade.ExplodeDestructible(destructible, attacker, position, settingsReference, explosionType))
						{
							hashSet.Add(destructible.NetworkId);
						}
					}
					else
					{
						InteractableCollider interactableCollider;
						DoorVariant doorVariant;
						bool flag2;
						if (collider.TryGetComponent<InteractableCollider>(out interactableCollider))
						{
							doorVariant = interactableCollider.Target as DoorVariant;
							flag2 = doorVariant != null;
						}
						else
						{
							flag2 = false;
						}
						if (flag2 && flag && hashSet2.Add(doorVariant.netId))
						{
							ExplosionGrenade.ExplodeDoor(doorVariant, position, settingsReference, attacker);
						}
					}
				}
				if (collider.attachedRigidbody != null)
				{
					ExplosionGrenade.ExplodeRigidbody(collider.attachedRigidbody, position, maxRadius, settingsReference);
				}
			}
			HashSetPool<uint>.Shared.Return(hashSet);
			HashSetPool<uint>.Shared.Return(hashSet2);
			Action<Footprint, Vector3, ExplosionGrenade> onExploded = ExplosionGrenade.OnExploded;
			if (onExploded != null)
			{
				onExploded(attacker, position, settingsReference);
			}
			ServerEvents.OnExplosionSpawned(new ExplosionSpawnedEventArgs(attacker.Hub, position, settingsReference, explosionType, flag));
			ExplosionGrenade.SetHostHitboxes(false);
		}

		private static void SetHostHitboxes(bool state)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			HitboxIdentity[] hitboxes = fpcRole.FpcModule.CharacterModelInstance.Hitboxes;
			for (int i = 0; i < hitboxes.Length; i++)
			{
				hitboxes[i].SetColliders(state);
			}
		}

		private static void ExplodeRigidbody(Rigidbody rb, Vector3 pos, float radius, ExplosionGrenade setts)
		{
			if (rb.isKinematic)
			{
				return;
			}
			if (Physics.Linecast(rb.gameObject.transform.position, pos, ThrownProjectile.HitBlockerMask))
			{
				return;
			}
			float num = Mathf.Clamp01(Mathf.InverseLerp(0.5f, 10f, rb.mass)) * 3f + 1f;
			rb.AddExplosionForce(setts._rigidbodyBaseForce / num, pos, radius, setts._rigidbodyLiftForce / num, ForceMode.VelocityChange);
		}

		private static bool ExplodeDestructible(IDestructible dest, Footprint attacker, Vector3 pos, ExplosionGrenade setts, ExplosionType explosionType)
		{
			if (Physics.Linecast(dest.CenterOfMass, pos, ThrownProjectile.HitBlockerMask))
			{
				return false;
			}
			Vector3 vector = dest.CenterOfMass - pos;
			float magnitude = vector.magnitude;
			float num = setts._playerDamageOverDistance.Evaluate(magnitude);
			ReferenceHub referenceHub;
			bool flag = ReferenceHub.TryGetHubNetID(dest.NetworkId, out referenceHub);
			if (flag && referenceHub.GetRoleId().GetTeam() == Team.SCPs)
			{
				num *= setts.ScpDamageMultiplier;
				HumeShieldStat humeShieldStat;
				if (referenceHub.playerStats.TryGetModule<HumeShieldStat>(out humeShieldStat))
				{
					if (num * setts._humeShieldMultipler < humeShieldStat.CurValue)
					{
						num *= setts._humeShieldMultipler;
					}
					else
					{
						num += humeShieldStat.CurValue / setts._humeShieldMultipler;
					}
				}
			}
			Vector3 vector2 = (1f - magnitude / setts.MaxRadius) * (vector / magnitude) * setts._rigidbodyBaseForce + Vector3.up * setts._rigidbodyLiftForce;
			if (num > 0f && dest.Damage(num, new ExplosionDamageHandler(attacker, vector2, num, 50, explosionType), dest.CenterOfMass) && flag)
			{
				float num2 = setts._effectDurationOverDistance.Evaluate(magnitude);
				bool flag2 = attacker.Hub == referenceHub;
				if (num2 > 0f && (flag2 || HitboxIdentity.IsDamageable(attacker.Role, referenceHub.GetRoleId())))
				{
					float minimalDuration = setts._minimalDuration;
					ExplosionGrenade.TriggerEffect<Burned>(referenceHub, num2 * setts._burnedDuration, minimalDuration);
					ExplosionGrenade.TriggerEffect<Deafened>(referenceHub, num2 * setts._deafenedDuration, minimalDuration);
					ExplosionGrenade.TriggerEffect<Concussed>(referenceHub, num2 * setts._concussedDuration, minimalDuration);
				}
				if (!flag2 && attacker.Hub != null)
				{
					Hitmarker.SendHitmarkerDirectly(attacker.Hub, 1f, true);
				}
			}
			return true;
		}

		private static void ExplodeDoor(DoorVariant dv, Vector3 pos, ExplosionGrenade setts, Footprint attacker)
		{
			IDamageableDoor damageableDoor = dv as IDamageableDoor;
			if (damageableDoor == null)
			{
				return;
			}
			float num = Vector3.Distance(dv.transform.position, pos);
			damageableDoor.ServerDamage(setts._doorDamageOverDistance.Evaluate(num), DoorDamageType.Grenade, attacker);
		}

		private static void TriggerEffect<T>(ReferenceHub hub, float duration, float minimal) where T : StatusEffectBase
		{
			if (duration < minimal)
			{
				return;
			}
			hub.playerEffectsController.EnableEffect<T>(duration, true);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static ExplosionGrenade()
		{
			ExplosionGrenade.OnExploded = delegate(Footprint attacker, Vector3 position, ExplosionGrenade settingsReference)
			{
			};
		}

		public override bool Weaved()
		{
			return true;
		}

		[Header("Hitreg")]
		public LayerMask DetectionMask;

		public float MaxRadius;

		public float ScpDamageMultiplier;

		[Header("Curves")]
		[SerializeField]
		private AnimationCurve _playerDamageOverDistance;

		[SerializeField]
		private AnimationCurve _effectDurationOverDistance;

		[SerializeField]
		private AnimationCurve _doorDamageOverDistance;

		[SerializeField]
		private AnimationCurve _shakeOverDistance;

		[Header("Player Effects")]
		[SerializeField]
		private float _burnedDuration;

		[SerializeField]
		private float _deafenedDuration;

		[SerializeField]
		private float _concussedDuration;

		[SerializeField]
		private float _minimalDuration;

		[Header("Physics")]
		[SerializeField]
		private float _rigidbodyBaseForce;

		[SerializeField]
		private float _rigidbodyLiftForce;

		[SerializeField]
		private float _humeShieldMultipler;

		private const float MinimalMass = 0.5f;

		private const float MaxMass = 10f;

		private const float MassFactor = 3f;
	}
}
