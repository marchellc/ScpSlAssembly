using System;
using System.Collections.Generic;
using Footprinting;
using InventorySystem;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class FirearmDamageHandler : AttackerDamageHandler
	{
		public override float Damage { get; internal set; }

		public override Footprint Attacker { get; protected set; }

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return string.Concat(new string[]
				{
					"Shot by ",
					this.Attacker.Nickname,
					" with ",
					this.WeaponType.ToString(),
					" to the '",
					this.Hitbox.ToString(),
					"' hitbox."
				});
			}
		}

		public Firearm Firearm { get; private set; }

		public ItemType WeaponType { get; private set; }

		public ItemType AmmoType { get; private set; }

		public FirearmDamageHandler()
		{
		}

		public FirearmDamageHandler(Firearm firearm, float damage, float penetration, bool useHumanMutlipliers = true)
		{
			this.Damage = damage;
			this.Attacker = firearm.Footprint;
			this.Firearm = firearm;
			this.WeaponType = firearm.ItemTypeId;
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			this.AmmoType = (firearm.TryGetModule(out primaryAmmoContainerModule, true) ? primaryAmmoContainerModule.AmmoType : ItemType.None);
			this._penetration = penetration;
			this._useHumanHitboxes = useHumanMutlipliers;
			Vector3 forward = firearm.Owner.PlayerCameraReference.forward;
			this._hitDirectionX = (sbyte)Mathf.RoundToInt(forward.x * 127f);
			this._hitDirectionZ = (sbyte)Mathf.RoundToInt(forward.z * 127f);
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteSByte((sbyte)this.WeaponType);
			writer.WriteSByte((sbyte)this.AmmoType);
			writer.WriteByte((byte)this.Hitbox);
			writer.WriteSByte(this._hitDirectionX);
			writer.WriteSByte(this._hitDirectionZ);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this.WeaponType = (ItemType)reader.ReadSByte();
			this.AmmoType = (ItemType)reader.ReadSByte();
			this.Hitbox = (HitboxType)reader.ReadByte();
			this._hitDirectionX = reader.ReadSByte();
			this._hitDirectionZ = reader.ReadSByte();
			AmmoItem ammoItem;
			if (InventoryItemLoader.TryGetItem<AmmoItem>(this.AmmoType, out ammoItem))
			{
				this._ammoName = ammoItem.Name;
			}
		}

		public override void ProcessRagdoll(BasicRagdoll ragdoll)
		{
			base.ProcessRagdoll(ragdoll);
			float num;
			if (!FirearmDamageHandler.HitboxToForce.TryGetValue(this.Hitbox, out num))
			{
				return;
			}
			DynamicRagdoll dynamicRagdoll = ragdoll as DynamicRagdoll;
			if (dynamicRagdoll == null)
			{
				return;
			}
			float num3;
			float num2 = num * (FirearmDamageHandler.AmmoToForce.TryGetValue(this.AmmoType, out num3) ? num3 : 1f);
			Rigidbody[] linkedRigidbodies = dynamicRagdoll.LinkedRigidbodies;
			for (int i = 0; i < linkedRigidbodies.Length; i++)
			{
				linkedRigidbodies[i].AddForce(num2 * 127f * 0.1f * Vector3.up, ForceMode.VelocityChange);
			}
			Vector3 vector = new Vector3((float)this._hitDirectionX, 0f, (float)this._hitDirectionZ) * num2;
			foreach (HitboxData hitboxData in dynamicRagdoll.Hitboxes)
			{
				if (hitboxData.RelatedHitbox == this.Hitbox)
				{
					hitboxData.Target.AddForce(vector, ForceMode.VelocityChange);
				}
			}
		}

		protected override void ProcessDamage(ReferenceHub ply)
		{
			if (!this._useHumanHitboxes && ply.IsHuman())
			{
				this.Hitbox = HitboxType.Body;
			}
			IArmoredRole armoredRole = ply.roleManager.CurrentRole as IArmoredRole;
			if (armoredRole != null)
			{
				int armorEfficacy = armoredRole.GetArmorEfficacy(this.Hitbox);
				int num = Mathf.RoundToInt(this._penetration * 100f);
				this.Damage = BodyArmorUtils.ProcessDamage(armorEfficacy, this.Damage, num);
			}
			float num2;
			if (this._useHumanHitboxes && FirearmDamageHandler.HitboxDamageMultipliers.TryGetValue(this.Hitbox, out num2))
			{
				this.Damage *= num2;
			}
			base.ProcessDamage(ply);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static FirearmDamageHandler()
		{
			Dictionary<HitboxType, float> dictionary = new Dictionary<HitboxType, float>();
			dictionary[HitboxType.Body] = 0.08f;
			dictionary[HitboxType.Headshot] = 0.08f;
			dictionary[HitboxType.Limb] = 0.016f;
			FirearmDamageHandler.HitboxToForce = dictionary;
			Dictionary<HitboxType, float> dictionary2 = new Dictionary<HitboxType, float>();
			dictionary2[HitboxType.Headshot] = 2f;
			dictionary2[HitboxType.Limb] = 0.7f;
			FirearmDamageHandler.HitboxDamageMultipliers = dictionary2;
			Dictionary<ItemType, float> dictionary3 = new Dictionary<ItemType, float>();
			dictionary3[ItemType.Ammo12gauge] = 1.9f;
			dictionary3[ItemType.Ammo44cal] = 1.2f;
			dictionary3[ItemType.Ammo9x19] = 0.7f;
			FirearmDamageHandler.AmmoToForce = dictionary3;
		}

		private string _ammoName;

		private sbyte _hitDirectionX;

		private sbyte _hitDirectionZ;

		private readonly float _penetration;

		private readonly string _deathReasonFormat;

		private readonly bool _useHumanHitboxes;

		private static readonly Dictionary<HitboxType, float> HitboxToForce;

		private static readonly Dictionary<HitboxType, float> HitboxDamageMultipliers;

		private static readonly Dictionary<ItemType, float> AmmoToForce;

		private const float UpwardVelocityFactor = 0.1f;
	}
}
