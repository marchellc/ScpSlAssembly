using System;
using System.Collections.Generic;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules;

public class DisruptorHitregModule : HitscanHitregModuleBase
{
	private static readonly RaycastHit[] NonAllocHits = new RaycastHit[32];

	private static readonly List<RaycastHit> SortedByDistanceHits = new List<RaycastHit>();

	private const float ExplosionOffset = 0.15f;

	private bool _hasOwner;

	private DisruptorShotEvent _templateShotData;

	private Ray _lastShotRay;

	private int _serverPenetrations;

	[Header("Single Shot Settings")]
	[SerializeField]
	private float _singleShotBaseDamage;

	[SerializeField]
	private float _singleShotFalloffDistance;

	[SerializeField]
	private float _singleShotDivisionPerTarget;

	[SerializeField]
	private float _singleShotThickness;

	[SerializeField]
	private ExplosionGrenade _singleShotExplosionSettings;

	[Header("Rapid Fire Settings (per shot)")]
	[SerializeField]
	private float _rapidFireBaseDamage;

	[SerializeField]
	private float _rapidFireFalloffDistance;

	public override float BaseDamage
	{
		get
		{
			return this.LastFiringState switch
			{
				DisruptorActionModule.FiringState.FiringSingle => this._singleShotBaseDamage, 
				DisruptorActionModule.FiringState.FiringRapid => this._rapidFireBaseDamage, 
				_ => base.BaseDamage, 
			};
		}
		protected set
		{
			base.BaseDamage = value;
		}
	}

	public override float DamageFalloffDistance
	{
		get
		{
			return this.LastFiringState switch
			{
				DisruptorActionModule.FiringState.FiringSingle => this._singleShotFalloffDistance, 
				DisruptorActionModule.FiringState.FiringRapid => this._rapidFireFalloffDistance, 
				_ => base.DamageFalloffDistance, 
			};
		}
		protected set
		{
			base.DamageFalloffDistance = value;
		}
	}

	public override bool UseHitboxMultipliers => false;

	public DisruptorActionModule.FiringState LastFiringState => this.DisruptorShotData?.State ?? DisruptorActionModule.FiringState.None;

	private DisruptorShotEvent DisruptorShotData
	{
		get
		{
			if (!this._hasOwner)
			{
				return this._templateShotData;
			}
			return base.LastShotEvent as DisruptorShotEvent;
		}
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		this._hasOwner = true;
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		ItemIdentifier shotFirearm = new ItemIdentifier(base.Firearm.ItemTypeId, reader.ReadUShort());
		DisruptorActionModule.FiringState state = (DisruptorActionModule.FiringState)reader.ReadByte();
		ShotEventManager.Trigger(new DisruptorShotEvent(shotFirearm, default(Footprint), state));
	}

	protected override void Fire()
	{
		Ray ray = (this._lastShotRay = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy));
		HitscanResult resultNonAlloc = base.ResultNonAlloc;
		resultNonAlloc.Clear();
		switch (this.LastFiringState)
		{
		case DisruptorActionModule.FiringState.FiringSingle:
			this.PrescanSingle(ray, resultNonAlloc);
			break;
		case DisruptorActionModule.FiringState.FiringRapid:
			base.ServerAppendPrescan(ray, resultNonAlloc);
			break;
		}
		this.ServerApplyDamage(resultNonAlloc);
	}

	private void PrescanSingle(Ray ray, HitscanResult result)
	{
		this._serverPenetrations = 0;
		int count = Physics.SphereCastNonAlloc(ray, this._singleShotThickness, DisruptorHitregModule.NonAllocHits, this.DamageFalloffDistance + this.FullDamageDistance, HitscanHitregModuleBase.HitregMask);
		DisruptorHitregModule.SortedByDistanceHits.Clear();
		DisruptorHitregModule.SortedByDistanceHits.AddRange(new ArraySegment<RaycastHit>(DisruptorHitregModule.NonAllocHits, 0, count));
		DisruptorHitregModule.SortedByDistanceHits.Sort((RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
		RaycastHit? raycastHit = null;
		foreach (RaycastHit sortedByDistanceHit in DisruptorHitregModule.SortedByDistanceHits)
		{
			raycastHit = sortedByDistanceHit;
			if (sortedByDistanceHit.collider.TryGetComponent<IDestructible>(out var component))
			{
				if (this.ValidateTarget(component, result))
				{
					result.Destructibles.Add(new DestructibleHitPair(component, sortedByDistanceHit, ray));
					this._serverPenetrations++;
				}
			}
			else
			{
				if (!this.TryGetDoor(sortedByDistanceHit, out var _))
				{
					break;
				}
				result.Obstacles.Add(new HitRayPair(ray, sortedByDistanceHit));
			}
		}
		if (raycastHit.HasValue)
		{
			base.RestoreHitboxes();
			Vector3 position = raycastHit.Value.point + 0.15f * -ray.direction;
			ExplosionGrenade.Explode(this.DisruptorShotData.HitregFootprint, position, this._singleShotExplosionSettings, ExplosionType.Disruptor);
		}
	}

	private bool TryGetDoor(RaycastHit hit, out IDamageableDoor ret)
	{
		if (hit.transform.TryGetComponentInParent<DoorVariant>(out var comp) && comp is IDamageableDoor { IsDestroyed: false } damageableDoor)
		{
			ret = damageableDoor;
			return true;
		}
		ret = null;
		return false;
	}

	protected override void ServerApplyObstacleDamage(HitRayPair hitInfo, HitscanResult result)
	{
		base.ServerApplyObstacleDamage(hitInfo, result);
		if (this.TryGetDoor(hitInfo.Hit, out var ret) && ret.ServerDamage(this.BaseDamage, DoorDamageType.ParticleDisruptor))
		{
			result.OtherDamage += this.BaseDamage;
		}
	}

	protected override void ServerApplyDestructibleDamage(DestructibleHitPair target, HitscanResult result)
	{
		float num = this.DamageAtDistance(target.Hit.distance);
		num *= Mathf.Pow(1f / this._singleShotDivisionPerTarget, this._serverPenetrations);
		DisruptorDamageHandler handler = new DisruptorDamageHandler(this.DisruptorShotData, this._lastShotRay.direction, num);
		if (target.Destructible.Damage(num, handler, target.Hit.point))
		{
			result.RegisterDamage(target.Destructible, num, handler);
			base.ServerPlayImpactEffects(target.Raycast, num > 0f);
		}
	}

	protected override bool ValidateTarget(IDestructible target, HitscanResult resultThusFar)
	{
		if (base.ValidateTarget(target, resultThusFar))
		{
			return !resultThusFar.Destructibles.Any((DestructibleHitPair x) => x.Destructible.NetworkId == target.NetworkId);
		}
		return false;
	}

	public static void TemplateSimulateShot(DisruptorShotEvent data, BarrelTipExtension barrelTip)
	{
		if (InventoryItemLoader.TryGetItem<ParticleDisruptor>(ItemType.ParticleDisruptor, out var result) && result.TryGetModule<DisruptorHitregModule>(out var module))
		{
			ushort itemSerial = result.ItemSerial;
			result.ItemSerial = data.ItemId.SerialNumber;
			Ray ray = new Ray(barrelTip.WorldspacePosition, barrelTip.WorldspaceDirection);
			module._hasOwner = false;
			module._templateShotData = data;
			module._lastShotRay = ray;
			HitscanResult resultNonAlloc = module.ResultNonAlloc;
			resultNonAlloc.Clear();
			switch (data.State)
			{
			case DisruptorActionModule.FiringState.FiringSingle:
				module.PrescanSingle(ray, resultNonAlloc);
				break;
			case DisruptorActionModule.FiringState.FiringRapid:
				module.ServerAppendPrescan(ray, resultNonAlloc);
				break;
			}
			module.Footprint = data.HitregFootprint;
			module.ServerApplyDamage(resultNonAlloc);
			result.ItemSerial = itemSerial;
			module.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteUShort(data.ItemId.SerialNumber);
				x.WriteByte((byte)data.State);
			});
		}
	}
}
