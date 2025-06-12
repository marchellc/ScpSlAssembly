using System;
using System.Collections.Generic;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096HitHandler
{
	private static readonly Collider[] Hits = new Collider[32];

	private static readonly CachedLayerMask SolidObjectMask = new CachedLayerMask("Default", "Door", "Glass");

	private static readonly CachedLayerMask AttackHitMask = new CachedLayerMask("Hitbox", "Door", "Glass");

	private readonly Scp096TargetsTracker _targetCounter;

	private readonly HashSet<uint> _hitNetIDs;

	private readonly Scp096Role _scpRole;

	private readonly float _windowDamage;

	private readonly float _doorDamage;

	private readonly float _humanTargetDamage;

	private readonly float _humanNontargetDamage;

	private readonly Scp096DamageHandler.AttackType _damageType;

	public Scp096HitResult HitResult { get; private set; }

	public event Action<ReferenceHub> OnPlayerHit;

	public event Action<BreakableWindow> OnWindowHit;

	public event Action<IDamageableDoor> OnDoorHit;

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
		Footprint attacker = default(Footprint);
		if (this._scpRole.TryGetOwner(out var hub))
		{
			attacker = new Footprint(hub);
		}
		for (int i = 0; i < count; i++)
		{
			Collider collider = Scp096HitHandler.Hits[i];
			this.CheckDoorHit(collider, attacker);
			if (!collider.TryGetComponent<IDestructible>(out var component))
			{
				continue;
			}
			int layerMask = (int)Scp096HitHandler.SolidObjectMask & ~(1 << collider.gameObject.layer);
			if (Physics.Linecast(this._scpRole.CameraPosition, component.CenterOfMass, layerMask) || !this._hitNetIDs.Add(component.NetworkId))
			{
				continue;
			}
			if (component is BreakableWindow breakableWindow)
			{
				if (this.DealDamage(breakableWindow, this._windowDamage))
				{
					scp096HitResult |= Scp096HitResult.Window;
					this.OnWindowHit?.Invoke(breakableWindow);
				}
			}
			else
			{
				if (!(component is HitboxIdentity hitboxIdentity) || !HitboxIdentity.IsEnemy(Team.SCPs, hitboxIdentity.TargetHub.GetTeam()))
				{
					continue;
				}
				ReferenceHub targetHub = hitboxIdentity.TargetHub;
				bool flag = this._targetCounter.HasTarget(targetHub);
				if (this.DealDamage(hitboxIdentity, flag ? this._humanTargetDamage : this._humanNontargetDamage))
				{
					scp096HitResult |= Scp096HitResult.Human;
					this.OnPlayerHit?.Invoke(targetHub);
					if (!targetHub.IsAlive())
					{
						scp096HitResult |= Scp096HitResult.Lethal;
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
		Scp096DamageHandler handler = new Scp096DamageHandler(this._scpRole, dmg, this._damageType);
		return target.Damage(dmg, handler, this._scpRole.FpcModule.Position);
	}

	private void CheckDoorHit(Collider col, Footprint attacker)
	{
		IDamageableDoor componentInParent = col.GetComponentInParent<IDamageableDoor>();
		if (componentInParent != null && componentInParent is NetworkBehaviour networkBehaviour && this._hitNetIDs.Add(networkBehaviour.netId) && componentInParent.ServerDamage(this._doorDamage, DoorDamageType.Scp096, attacker))
		{
			this.HitResult |= Scp096HitResult.Door;
			this.OnDoorHit?.Invoke(componentInParent);
		}
	}
}
