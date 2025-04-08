using System;
using System.Collections.Generic;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096HitHandler
	{
		public event Action<ReferenceHub> OnPlayerHit;

		public event Action<BreakableWindow> OnWindowHit;

		public event Action<IDamageableDoor> OnDoorHit;

		public Scp096HitResult HitResult { get; private set; }

		public Scp096HitHandler(Scp096Role scpRole, Scp096DamageHandler.AttackType damageType, float windowDamage, float doorDamage, float humanTargetDamage, float humanNontargetDamage)
		{
			this._scpRole = scpRole;
			this._damageType = damageType;
			this._windowDamage = windowDamage;
			this._doorDamage = doorDamage;
			this._humanTargetDamage = humanTargetDamage;
			this._humanNontargetDamage = humanNontargetDamage;
			this.HitResult = Scp096HitResult.None;
			this._hitNetIDs = new HashSet<uint>();
			this._scpRole.SubroutineModule.TryGetSubroutine<Scp096TargetsTracker>(out this._targetCounter);
		}

		public void Clear()
		{
			this._hitNetIDs.Clear();
			this.HitResult = Scp096HitResult.None;
		}

		public Scp096HitResult DamageSphere(Vector3 position, float radius)
		{
			return this.ProcessHits(Physics.OverlapSphereNonAlloc(position, radius, Scp096HitHandler.Hits, Scp096HitHandler.AttackHitMask));
		}

		public Scp096HitResult DamageBox(Vector3 position, Vector3 halfExtents, Quaternion orientation)
		{
			return this.ProcessHits(Physics.OverlapBoxNonAlloc(position, halfExtents, Scp096HitHandler.Hits, orientation, Scp096HitHandler.AttackHitMask));
		}

		private Scp096HitResult ProcessHits(int count)
		{
			Scp096HitResult scp096HitResult = Scp096HitResult.None;
			Footprint footprint = default(Footprint);
			ReferenceHub referenceHub;
			if (this._scpRole.TryGetOwner(out referenceHub))
			{
				footprint = new Footprint(referenceHub);
			}
			for (int i = 0; i < count; i++)
			{
				Collider collider = Scp096HitHandler.Hits[i];
				this.CheckDoorHit(collider, footprint);
				IDestructible destructible;
				if (collider.TryGetComponent<IDestructible>(out destructible))
				{
					int num = Scp096HitHandler.SolidObjectMask & ~(1 << collider.gameObject.layer);
					if (!Physics.Linecast(this._scpRole.CameraPosition, destructible.CenterOfMass, num) && this._hitNetIDs.Add(destructible.NetworkId))
					{
						BreakableWindow breakableWindow = destructible as BreakableWindow;
						if (breakableWindow != null)
						{
							if (this.DealDamage(breakableWindow, this._windowDamage))
							{
								scp096HitResult |= Scp096HitResult.Window;
								Action<BreakableWindow> onWindowHit = this.OnWindowHit;
								if (onWindowHit != null)
								{
									onWindowHit(breakableWindow);
								}
							}
						}
						else
						{
							HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
							if (hitboxIdentity != null && HitboxIdentity.IsEnemy(Team.SCPs, hitboxIdentity.TargetHub.GetTeam()))
							{
								ReferenceHub targetHub = hitboxIdentity.TargetHub;
								bool flag = this._targetCounter.HasTarget(targetHub);
								if (this.DealDamage(hitboxIdentity, flag ? this._humanTargetDamage : this._humanNontargetDamage))
								{
									scp096HitResult |= Scp096HitResult.Human;
									Action<ReferenceHub> onPlayerHit = this.OnPlayerHit;
									if (onPlayerHit != null)
									{
										onPlayerHit(targetHub);
									}
									if (!targetHub.IsAlive())
									{
										scp096HitResult |= Scp096HitResult.Lethal;
									}
								}
							}
						}
					}
				}
			}
			this.HitResult |= scp096HitResult;
			return scp096HitResult;
		}

		private bool DealDamage(IDestructible target, float dmg)
		{
			if (dmg <= 0f)
			{
				return false;
			}
			Scp096DamageHandler scp096DamageHandler = new Scp096DamageHandler(this._scpRole, dmg, this._damageType);
			return target.Damage(dmg, scp096DamageHandler, this._scpRole.FpcModule.Position);
		}

		private void CheckDoorHit(Collider col, Footprint attacker)
		{
			IDamageableDoor componentInParent = col.GetComponentInParent<IDamageableDoor>();
			if (componentInParent == null)
			{
				return;
			}
			NetworkBehaviour networkBehaviour = componentInParent as NetworkBehaviour;
			if (networkBehaviour == null || !this._hitNetIDs.Add(networkBehaviour.netId))
			{
				return;
			}
			if (!componentInParent.ServerDamage(this._doorDamage, DoorDamageType.Scp096, attacker))
			{
				return;
			}
			this.HitResult |= Scp096HitResult.Door;
			Action<IDamageableDoor> onDoorHit = this.OnDoorHit;
			if (onDoorHit == null)
			{
				return;
			}
			onDoorHit(componentInParent);
		}

		private static readonly Collider[] Hits = new Collider[32];

		private static readonly CachedLayerMask SolidObjectMask = new CachedLayerMask(new string[] { "Default", "Door", "Glass" });

		private static readonly CachedLayerMask AttackHitMask = new CachedLayerMask(new string[] { "Hitbox", "Door", "Glass" });

		private readonly Scp096TargetsTracker _targetCounter;

		private readonly HashSet<uint> _hitNetIDs;

		private readonly Scp096Role _scpRole;

		private readonly float _windowDamage;

		private readonly float _doorDamage;

		private readonly float _humanTargetDamage;

		private readonly float _humanNontargetDamage;

		private readonly Scp096DamageHandler.AttackType _damageType;
	}
}
