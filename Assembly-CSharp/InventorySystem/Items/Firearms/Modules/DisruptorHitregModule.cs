using System;
using System.Collections.Generic;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules
{
	public class DisruptorHitregModule : HitscanHitregModuleBase
	{
		public override float BaseDamage
		{
			get
			{
				DisruptorActionModule.FiringState lastFiringState = this.LastFiringState;
				float num;
				if (lastFiringState != DisruptorActionModule.FiringState.FiringRapid)
				{
					if (lastFiringState == DisruptorActionModule.FiringState.FiringSingle)
					{
						num = this._singleShotBaseDamage;
					}
					else
					{
						num = base.BaseDamage;
					}
				}
				else
				{
					num = this._rapidFireBaseDamage;
				}
				return num;
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
				DisruptorActionModule.FiringState lastFiringState = this.LastFiringState;
				float num;
				if (lastFiringState != DisruptorActionModule.FiringState.FiringRapid)
				{
					if (lastFiringState == DisruptorActionModule.FiringState.FiringSingle)
					{
						num = this._singleShotFalloffDistance;
					}
					else
					{
						num = base.DamageFalloffDistance;
					}
				}
				else
				{
					num = this._rapidFireFalloffDistance;
				}
				return num;
			}
			protected set
			{
				base.DamageFalloffDistance = value;
			}
		}

		public override bool UseHitboxMultipliers
		{
			get
			{
				return false;
			}
		}

		public DisruptorActionModule.FiringState LastFiringState
		{
			get
			{
				DisruptorShotEvent disruptorShotData = this.DisruptorShotData;
				if (disruptorShotData == null)
				{
					return DisruptorActionModule.FiringState.None;
				}
				return disruptorShotData.State;
			}
		}

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
			ItemIdentifier itemIdentifier = new ItemIdentifier(base.Firearm.ItemTypeId, reader.ReadUShort());
			DisruptorActionModule.FiringState firingState = (DisruptorActionModule.FiringState)reader.ReadByte();
			ShotEventManager.Trigger(new DisruptorShotEvent(itemIdentifier, default(Footprint), firingState));
		}

		protected override void Fire()
		{
			float num = 0f;
			Ray ray = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
			this._lastShotRay = ray;
			DisruptorActionModule.FiringState lastFiringState = this.LastFiringState;
			if (lastFiringState != DisruptorActionModule.FiringState.FiringRapid)
			{
				if (lastFiringState == DisruptorActionModule.FiringState.FiringSingle)
				{
					this.ServerPerformSingle(ray, out num);
				}
			}
			else
			{
				base.ServerPerformHitscan(ray, out num);
			}
			this.SendHitmarker(num);
		}

		protected override float ServerProcessObstacleHit(RaycastHit hitInfo)
		{
			float num = base.ServerProcessObstacleHit(hitInfo);
			DoorVariant doorVariant;
			if (!hitInfo.collider.transform.TryGetComponentInParent(out doorVariant))
			{
				return num;
			}
			IDamageableDoor damageableDoor = doorVariant as IDamageableDoor;
			if (damageableDoor == null || damageableDoor.IsDestroyed)
			{
				return num;
			}
			if (!damageableDoor.ServerDamage(this.BaseDamage, DoorDamageType.ParticleDisruptor, default(Footprint)))
			{
				return num;
			}
			return num + this.BaseDamage;
		}

		protected override float ServerProcessTargetHit(IDestructible dest, RaycastHit hitInfo)
		{
			float num = this.DamageAtDistance(hitInfo.distance);
			num *= Mathf.Pow(1f / this._singleShotDivisionPerTarget, (float)this._serverPenetrations);
			DisruptorDamageHandler disruptorDamageHandler = new DisruptorDamageHandler(this.DisruptorShotData, this._lastShotRay.direction, num);
			if (!dest.Damage(num, disruptorDamageHandler, hitInfo.point))
			{
				return 0f;
			}
			HitboxIdentity hitboxIdentity = dest as HitboxIdentity;
			if (hitboxIdentity != null)
			{
				base.SendDamageIndicator(hitboxIdentity.TargetHub, num, this._lastShotRay.origin);
			}
			this.ServerLastDamagedTargets.Add(dest);
			return num;
		}

		protected override bool ValidateTarget(IDestructible target)
		{
			return base.ValidateTarget(target) && !this.ServerLastDamagedTargets.Any((IDestructible x) => x.NetworkId == target.NetworkId);
		}

		private void ServerPerformSingle(Ray ray, out float targetDamage)
		{
			targetDamage = 0f;
			this._serverPenetrations = 0;
			this.ServerLastDamagedTargets.Clear();
			int num = Physics.SphereCastNonAlloc(ray, this._singleShotThickness, DisruptorHitregModule.NonAllocHits, this.DamageFalloffDistance + this.FullDamageDistance, HitscanHitregModuleBase.HitregMask);
			DisruptorHitregModule.SortedByDistanceHits.Clear();
			DisruptorHitregModule.SortedByDistanceHits.AddRange(new ArraySegment<RaycastHit>(DisruptorHitregModule.NonAllocHits, 0, num));
			DisruptorHitregModule.SortedByDistanceHits.Sort((RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
			RaycastHit? raycastHit = null;
			foreach (RaycastHit raycastHit2 in DisruptorHitregModule.SortedByDistanceHits)
			{
				raycastHit = new RaycastHit?(raycastHit2);
				IDestructible destructible;
				if (raycastHit2.collider.TryGetComponent<IDestructible>(out destructible))
				{
					if (this.ValidateTarget(destructible))
					{
						targetDamage += this.ServerProcessTargetHit(destructible, raycastHit2);
						this._serverPenetrations++;
					}
				}
				else
				{
					float num2 = this.ServerProcessObstacleHit(raycastHit2);
					if (num2 <= 0f)
					{
						break;
					}
					targetDamage += num2;
					this._serverPenetrations++;
				}
			}
			if (raycastHit != null)
			{
				base.RestoreHitboxes();
				ImpactEffectsModule impactEffectsModule;
				if (base.Firearm.TryGetModule(out impactEffectsModule, true))
				{
					impactEffectsModule.ServerProcessHit(raycastHit.Value, ray.origin, targetDamage > 0f);
				}
				Vector3 vector = raycastHit.Value.point + 0.15f * -ray.direction;
				ExplosionGrenade.Explode(this.DisruptorShotData.HitregFootprint, vector, this._singleShotExplosionSettings, ExplosionType.Disruptor);
			}
		}

		public static void TemplateSimulateShot(DisruptorShotEvent data, BarrelTipExtension barrelTip)
		{
			ParticleDisruptor particleDisruptor;
			if (!InventoryItemLoader.TryGetItem<ParticleDisruptor>(ItemType.ParticleDisruptor, out particleDisruptor))
			{
				return;
			}
			DisruptorHitregModule disruptorHitregModule;
			if (!particleDisruptor.TryGetModule(out disruptorHitregModule, true))
			{
				return;
			}
			ushort itemSerial = particleDisruptor.ItemSerial;
			particleDisruptor.ItemSerial = data.ItemId.SerialNumber;
			Ray ray = new Ray(barrelTip.WorldspacePosition, barrelTip.WorldspaceDirection);
			disruptorHitregModule._hasOwner = false;
			disruptorHitregModule._templateShotData = data;
			disruptorHitregModule._lastShotRay = ray;
			DisruptorActionModule.FiringState state = data.State;
			if (state != DisruptorActionModule.FiringState.FiringRapid)
			{
				if (state == DisruptorActionModule.FiringState.FiringSingle)
				{
					float num;
					disruptorHitregModule.ServerPerformSingle(ray, out num);
				}
			}
			else
			{
				float num;
				disruptorHitregModule.ServerPerformHitscan(ray, out num);
			}
			particleDisruptor.ItemSerial = itemSerial;
			disruptorHitregModule.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteUShort(data.ItemId.SerialNumber);
				x.WriteByte((byte)data.State);
			}, true);
		}

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
	}
}
