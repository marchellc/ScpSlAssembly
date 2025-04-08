using System;
using System.Collections.Generic;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.Firearms.ShotEvents;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public abstract class HitscanHitregModuleBase : ModuleBase, IHitregModule, ICustomCrosshairItem, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule
	{
		public event Action ServerOnFired;

		public virtual float BaseDamage { get; protected set; }

		public virtual float BasePenetration { get; protected set; }

		public virtual float FullDamageDistance { get; protected set; }

		public virtual float DamageFalloffDistance { get; protected set; }

		public float BaseBulletInaccuracy { get; private set; }

		public virtual float DisplayDamage
		{
			get
			{
				return this.EffectiveDamage;
			}
		}

		public virtual float DisplayPenetration
		{
			get
			{
				return this.EffectivePenetration;
			}
		}

		public virtual float HitmarkerSize
		{
			get
			{
				return 1f;
			}
		}

		public virtual bool UseHitboxMultipliers
		{
			get
			{
				return true;
			}
		}

		public virtual Type CrosshairType
		{
			get
			{
				return typeof(SingleBulletFirearmCrosshair);
			}
		}

		public virtual float Inaccuracy
		{
			get
			{
				return this.BaseBulletInaccuracy * base.Firearm.AttachmentsValue(AttachmentParam.BulletInaccuracyMultiplier);
			}
		}

		public virtual DisplayInaccuracyValues DisplayInaccuracy
		{
			get
			{
				return new DisplayInaccuracyValues(0f, 0f, 0f, this.Inaccuracy);
			}
		}

		private protected ReferenceHub PrimaryTarget { protected get; private set; }

		private protected ShotEvent LastShotEvent { protected get; private set; }

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
					IInaccuracyProviderModule inaccuracyProviderModule = modules[i] as IInaccuracyProviderModule;
					if (inaccuracyProviderModule != null)
					{
						num += inaccuracyProviderModule.Inaccuracy;
					}
				}
				return num;
			}
		}

		private ReferenceHub Owner
		{
			get
			{
				return base.Firearm.Owner;
			}
		}

		private float EffectiveDamage
		{
			get
			{
				return this.BaseDamage * base.Firearm.AttachmentsValue(AttachmentParam.DamageMultiplier);
			}
		}

		private float EffectivePenetration
		{
			get
			{
				return this.BasePenetration * base.Firearm.AttachmentsValue(AttachmentParam.PenetrationMultiplier);
			}
		}

		public void Fire(ReferenceHub primaryTarget, ShotEvent shotEvent)
		{
			this.PrimaryTarget = primaryTarget;
			this.LastShotEvent = shotEvent;
			this.EnableSelfDamageProtection();
			this.Fire();
			Action serverOnFired = this.ServerOnFired;
			if (serverOnFired != null)
			{
				serverOnFired();
			}
			this.RestoreHitboxes();
		}

		protected abstract void Fire();

		protected Ray RandomizeRay(Ray ray, float angle)
		{
			float num = Mathf.Max(global::UnityEngine.Random.value, global::UnityEngine.Random.value);
			Vector3 vector = global::UnityEngine.Random.insideUnitSphere * num;
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

		protected void SendDamageIndicator(ReferenceHub receiver, float dmgDealt)
		{
			IFpcRole fpcRole = this.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			this.SendDamageIndicator(receiver, dmgDealt, fpcRole.FpcModule.Position);
		}

		protected void SendDamageIndicator(ReferenceHub receiver, float dmgDealt, Vector3 position)
		{
			new DamageIndicatorMessage(dmgDealt, position).SendToSpectatorsOf(receiver, true);
		}

		protected bool ServerPerformHitscan(Ray targetRay, out float targetDamage)
		{
			this.ServerLastDamagedTargets.Clear();
			float num = this.DamageFalloffDistance + this.FullDamageDistance;
			targetDamage = 0f;
			RaycastHit raycastHit;
			if (!Physics.Raycast(targetRay, out raycastHit, num, HitscanHitregModuleBase.HitregMask))
			{
				return false;
			}
			IDestructible destructible;
			if (raycastHit.collider.TryGetComponent<IDestructible>(out destructible))
			{
				if (!this.ValidateTarget(destructible))
				{
					return false;
				}
				targetDamage = this.ServerProcessTargetHit(destructible, raycastHit);
			}
			else
			{
				targetDamage = this.ServerProcessObstacleHit(raycastHit);
			}
			ImpactEffectsModule impactEffectsModule;
			if (!base.Firearm.TryGetModule(out impactEffectsModule, true))
			{
				return true;
			}
			impactEffectsModule.ServerProcessHit(raycastHit, targetRay.origin, targetDamage > 0f);
			return true;
		}

		protected virtual float ServerProcessObstacleHit(RaycastHit hitInfo)
		{
			return 0f;
		}

		protected virtual float ServerProcessTargetHit(IDestructible dest, RaycastHit hitInfo)
		{
			float num = this.DamageAtDistance(hitInfo.distance);
			FirearmDamageHandler firearmDamageHandler = new FirearmDamageHandler(base.Firearm, num, this.EffectivePenetration, this.UseHitboxMultipliers);
			if (!dest.Damage(num, firearmDamageHandler, hitInfo.point))
			{
				return 0f;
			}
			HitboxIdentity hitboxIdentity = dest as HitboxIdentity;
			if (hitboxIdentity != null)
			{
				this.SendDamageIndicator(hitboxIdentity.TargetHub, num);
			}
			this.ServerLastDamagedTargets.Add(dest);
			return num;
		}

		protected virtual void SendHitmarker(float damageDealt)
		{
			float num = damageDealt * this.HitmarkerSize / this.EffectiveDamage;
			if (num <= 0f)
			{
				return;
			}
			foreach (IDestructible destructible in this.ServerLastDamagedTargets)
			{
				HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
				if (hitboxIdentity != null)
				{
					FirearmDamageHandler firearmDamageHandler = new FirearmDamageHandler(base.Firearm, damageDealt, this.EffectiveDamage, this.UseHitboxMultipliers);
					Hitmarker.SendHitmarkerConditionally(num, firearmDamageHandler, hitboxIdentity.TargetHub);
				}
				else
				{
					Hitmarker.SendHitmarkerDirectly(this.Owner, num, true);
				}
			}
		}

		protected virtual bool ValidateTarget(IDestructible target)
		{
			HitboxIdentity hitboxIdentity = target as HitboxIdentity;
			return hitboxIdentity == null || this.PrimaryTarget == null || hitboxIdentity.TargetHub == this.PrimaryTarget || hitboxIdentity.TargetHub.GetFaction() != this.PrimaryTarget.GetFaction();
		}

		protected void EnableSelfDamageProtection()
		{
			this.SetHitboxes(false);
		}

		protected void RestoreHitboxes()
		{
			this.SetHitboxes(true);
		}

		private void SetHitboxes(bool restore)
		{
			if (this.Owner == null)
			{
				return;
			}
			HitscanHitregModuleBase.ToggleColliders(this.Owner, !this.Owner.isLocalPlayer && restore);
			if (this.PrimaryTarget == null || !this.PrimaryTarget.isLocalPlayer)
			{
				return;
			}
			HitscanHitregModuleBase.ToggleColliders(this.PrimaryTarget, !restore);
		}

		private static void ToggleColliders(ReferenceHub target, bool state)
		{
			IFpcRole fpcRole = target.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			fpcRole.FpcModule.CharacterModelInstance.Hitboxes.ForEach(delegate(HitboxIdentity x)
			{
				x.SetColliders(state);
			});
		}

		public static readonly CachedLayerMask HitregMask = new CachedLayerMask(new string[] { "Default", "Hitbox", "Glass", "CCTV", "Door" });

		protected readonly HashSet<IDestructible> ServerLastDamagedTargets = new HashSet<IDestructible>();
	}
}
