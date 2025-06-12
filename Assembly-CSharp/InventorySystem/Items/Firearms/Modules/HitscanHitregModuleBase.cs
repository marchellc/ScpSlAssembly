using System;
using System.Collections.Generic;
using Footprinting;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public abstract class HitscanHitregModuleBase : ModuleBase, IHitregModule, ICustomCrosshairItem, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule
{
	public static readonly CachedLayerMask HitregMask = new CachedLayerMask("Default", "Hitbox", "Glass", "CCTV", "Door");

	private HitscanResult _resultNonAlloc;

	private ImpactEffectsModule _impactEffectsModule;

	private Footprint _cachedFootprint;

	private bool _footprintCacheSet;

	private float? _scheduledHitmarker;

	[field: SerializeField]
	public virtual float BaseDamage { get; protected set; }

	[field: SerializeField]
	[field: Range(0f, 1f)]
	public virtual float BasePenetration { get; protected set; }

	[field: SerializeField]
	public virtual float FullDamageDistance { get; protected set; }

	[field: SerializeField]
	public virtual float DamageFalloffDistance { get; protected set; }

	[field: SerializeField]
	public virtual float BaseBulletInaccuracy { get; private set; }

	public virtual float DisplayDamage => this.EffectiveDamage;

	public virtual float DisplayPenetration => this.EffectivePenetration;

	public virtual bool UseHitboxMultipliers => true;

	public virtual Type CrosshairType => typeof(SingleBulletFirearmCrosshair);

	public virtual float Inaccuracy => this.BaseBulletInaccuracy * base.Firearm.AttachmentsValue(AttachmentParam.BulletInaccuracyMultiplier);

	public virtual DisplayInaccuracyValues DisplayInaccuracy => new DisplayInaccuracyValues(0f, 0f, 0f, this.Inaccuracy);

	protected ReferenceHub PrimaryTarget { get; private set; }

	protected ShotEvent LastShotEvent { get; private set; }

	protected HitscanResult ResultNonAlloc => this._resultNonAlloc ?? (this._resultNonAlloc = new HitscanResult());

	protected Ray ForwardRay
	{
		get
		{
			Transform playerCameraReference = this.Owner.PlayerCameraReference;
			return new Ray(playerCameraReference.position, playerCameraReference.forward);
		}
	}

	protected float CurrentInaccuracy
	{
		get
		{
			float num = 0f;
			ModuleBase[] modules = base.Firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				if (modules[i] is IInaccuracyProviderModule inaccuracyProviderModule)
				{
					num += inaccuracyProviderModule.Inaccuracy;
				}
			}
			return num;
		}
	}

	protected virtual float EffectiveDamage => this.BaseDamage * base.Firearm.AttachmentsValue(AttachmentParam.DamageMultiplier);

	protected virtual float EffectivePenetration => this.BasePenetration * base.Firearm.AttachmentsValue(AttachmentParam.PenetrationMultiplier);

	protected Footprint Footprint
	{
		get
		{
			if (!this._footprintCacheSet)
			{
				this._footprintCacheSet = true;
				this._cachedFootprint = new Footprint(this.Owner);
			}
			return this._cachedFootprint;
		}
		set
		{
			this._cachedFootprint = value;
			this._footprintCacheSet = true;
		}
	}

	private ReferenceHub Owner => base.Firearm.Owner;

	public event Action ServerOnFired;

	public void Fire(ReferenceHub primaryTarget, ShotEvent shotEvent)
	{
		this.PrimaryTarget = primaryTarget;
		this.LastShotEvent = shotEvent;
		this.EnableSelfDamageProtection();
		this.Fire();
		this.ServerOnFired?.Invoke();
		this.RestoreHitboxes();
	}

	protected override void OnInit()
	{
		base.OnInit();
		if (!base.Firearm.TryGetModule<ImpactEffectsModule>(out this._impactEffectsModule))
		{
			this._impactEffectsModule = null;
		}
	}

	internal override void AlwaysUpdate()
	{
		base.AlwaysUpdate();
		if (this._scheduledHitmarker.HasValue)
		{
			float num = this.HitmarkerSizeAtDamage(this._scheduledHitmarker.Value);
			if (num > 0f)
			{
				Hitmarker.SendHitmarkerDirectly(this.Footprint.Hub, num);
			}
			this._scheduledHitmarker = null;
		}
	}

	protected abstract void Fire();

	protected Ray RandomizeRay(Ray ray, float angle)
	{
		float num = Mathf.Max(UnityEngine.Random.value, UnityEngine.Random.value);
		Vector3 vector = UnityEngine.Random.insideUnitSphere * num;
		ray.direction = Quaternion.Euler(angle * vector) * ray.direction;
		return ray;
	}

	protected virtual float DamageAtDistance(float dist)
	{
		dist -= this.FullDamageDistance;
		if (dist <= 0f)
		{
			return this.EffectiveDamage;
		}
		float num = dist / this.DamageFalloffDistance;
		return this.EffectiveDamage * Mathf.Clamp01(1f - num);
	}

	protected virtual float HitmarkerSizeAtDamage(float damage)
	{
		return damage / this.EffectiveDamage;
	}

	protected void SendDamageIndicator(uint receiverNetId, float dmgDealt)
	{
		if (this.Footprint.Hub.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			this.SendDamageIndicator(receiverNetId, dmgDealt, fpcRole.FpcModule.Position);
		}
	}

	protected void SendDamageIndicator(uint receiverNetId, float dmgDealt, Vector3 position)
	{
		if (ReferenceHub.TryGetHubNetID(receiverNetId, out var hub))
		{
			new DamageIndicatorMessage(dmgDealt, position).SendToSpectatorsOf(hub, includeTarget: true);
		}
	}

	protected HitscanResult ServerPrescan(Ray targetRay, bool nonAlloc = true)
	{
		HitscanResult hitscanResult;
		if (nonAlloc)
		{
			hitscanResult = this.ResultNonAlloc;
			hitscanResult.Clear();
		}
		else
		{
			hitscanResult = new HitscanResult();
		}
		this.ServerAppendPrescan(targetRay, hitscanResult);
		return hitscanResult;
	}

	protected void ServerAppendPrescan(Ray targetRay, HitscanResult toAppend)
	{
		float maxDistance = this.DamageFalloffDistance + this.FullDamageDistance;
		if (!Physics.Raycast(targetRay, out var hitInfo, maxDistance, HitscanHitregModuleBase.HitregMask))
		{
			return;
		}
		if (hitInfo.collider.TryGetComponent<IDestructible>(out var component))
		{
			if (this.ValidateTarget(component, toAppend))
			{
				toAppend.Destructibles.Add(new DestructibleHitPair(component, hitInfo, targetRay));
			}
		}
		else
		{
			toAppend.Obstacles.Add(new HitRayPair(targetRay, hitInfo));
		}
	}

	protected virtual void ServerApplyDamage(HitscanResult result)
	{
		foreach (DestructibleHitPair destructible in result.Destructibles)
		{
			this.ServerApplyDestructibleDamage(destructible, result);
		}
		foreach (HitRayPair obstacle in result.Obstacles)
		{
			this.ServerApplyObstacleDamage(obstacle, result);
		}
		this.ServerSendAllIndicators(result);
	}

	protected virtual void ServerApplyObstacleDamage(HitRayPair hitInfo, HitscanResult result)
	{
		this.ServerPlayImpactEffects(hitInfo, allowBlood: false);
	}

	protected virtual void ServerApplyDestructibleDamage(DestructibleHitPair target, HitscanResult result)
	{
		float num = this.DamageAtDistance(target.Hit.distance);
		AttackerDamageHandler handler = this.GetHandler(num);
		IDestructible destructible = target.Destructible;
		if (destructible is HitboxIdentity hitboxIdentity && !hitboxIdentity.TargetHub.IsAlive())
		{
			result.RegisterDamage(destructible, num, handler);
			return;
		}
		Vector3 point = target.Hit.point;
		if (destructible.Damage(num, handler, point))
		{
			result.RegisterDamage(destructible, num, handler);
			this.ServerPlayImpactEffects(target.Raycast, num > 0f);
		}
	}

	protected virtual void ServerSendAllIndicators(HitscanResult result)
	{
		HashSet<uint> hashSet = HashSetPool<uint>.Shared.Rent();
		this.ServerAddHitmarkerDamage(result.OtherDamage);
		foreach (DestructibleDamageRecord damagedDestructible in result.DamagedDestructibles)
		{
			IDestructible destructible = damagedDestructible.Destructible;
			if (hashSet.Add(destructible.NetworkId))
			{
				float dmgDealt = result.CountDamage(destructible);
				this.SendDamageIndicator(destructible.NetworkId, dmgDealt);
			}
			if (!(destructible is HitboxIdentity hitboxIdentity))
			{
				this.ServerAddHitmarkerDamage(damagedDestructible.AppliedDamage);
			}
			else if (Hitmarker.CheckHitmarkerPerms(damagedDestructible.Handler, hitboxIdentity.TargetHub))
			{
				this.ServerAddHitmarkerDamage(damagedDestructible.AppliedDamage);
			}
		}
		this.AlwaysUpdate();
		HashSetPool<uint>.Shared.Return(hashSet);
	}

	protected virtual bool ValidateTarget(IDestructible target, HitscanResult resultThusFar)
	{
		if (!(target is HitboxIdentity hitboxIdentity))
		{
			return true;
		}
		if (this.PrimaryTarget == null)
		{
			return true;
		}
		if (hitboxIdentity.TargetHub == this.PrimaryTarget)
		{
			return true;
		}
		if (HitboxIdentity.IsEnemy(this.Owner, hitboxIdentity.TargetHub))
		{
			return true;
		}
		return false;
	}

	protected virtual AttackerDamageHandler GetHandler(float damageDealt)
	{
		return new FirearmDamageHandler(base.Firearm, damageDealt, this.EffectivePenetration, this.UseHitboxMultipliers);
	}

	protected void ServerAddHitmarkerDamage(float damageDealt)
	{
		float valueOrDefault = this._scheduledHitmarker.GetValueOrDefault();
		if (!this._scheduledHitmarker.HasValue)
		{
			valueOrDefault = 0f;
			this._scheduledHitmarker = valueOrDefault;
		}
		this._scheduledHitmarker += damageDealt;
	}

	protected void EnableSelfDamageProtection()
	{
		this.SetHitboxes(restore: false);
	}

	protected void RestoreHitboxes()
	{
		this.SetHitboxes(restore: true);
	}

	protected void ServerPlayImpactEffects(HitRayPair rc, bool allowBlood)
	{
		if ((object)this._impactEffectsModule != null)
		{
			this._impactEffectsModule.ServerProcessHit(rc.Hit, rc.Ray.origin, allowBlood);
		}
	}

	private void SetHitboxes(bool restore)
	{
		if (!(this.Owner == null))
		{
			HitscanHitregModuleBase.ToggleColliders(this.Owner, !this.Owner.isLocalPlayer && restore);
			if (!(this.PrimaryTarget == null) && this.PrimaryTarget.isLocalPlayer)
			{
				HitscanHitregModuleBase.ToggleColliders(this.PrimaryTarget, !restore);
			}
		}
	}

	private static void ToggleColliders(ReferenceHub target, bool state)
	{
		if (target.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			fpcRole.FpcModule.CharacterModelInstance.Hitboxes.ForEach(delegate(HitboxIdentity x)
			{
				x.SetColliders(state);
			});
		}
	}
}
