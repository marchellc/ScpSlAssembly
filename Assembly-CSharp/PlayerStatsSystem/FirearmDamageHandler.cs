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

namespace PlayerStatsSystem;

public class FirearmDamageHandler : AttackerDamageHandler
{
	private string _ammoName;

	private sbyte _hitDirectionX;

	private sbyte _hitDirectionZ;

	private readonly float _penetration;

	private readonly string _deathReasonFormat;

	private readonly bool _useHumanHitboxes;

	private static readonly Dictionary<HitboxType, float> HitboxToForce = new Dictionary<HitboxType, float>
	{
		[HitboxType.Body] = 0.08f,
		[HitboxType.Headshot] = 0.08f,
		[HitboxType.Limb] = 0.016f
	};

	private static readonly Dictionary<HitboxType, float> HitboxDamageMultipliers = new Dictionary<HitboxType, float>
	{
		[HitboxType.Headshot] = 2f,
		[HitboxType.Limb] = 0.7f
	};

	private static readonly Dictionary<ItemType, float> AmmoToForce = new Dictionary<ItemType, float>
	{
		[ItemType.Ammo12gauge] = 1.9f,
		[ItemType.Ammo44cal] = 1.2f,
		[ItemType.Ammo9x19] = 0.7f
	};

	private const float UpwardVelocityFactor = 0.1f;

	public override float Damage { get; set; }

	public override Footprint Attacker { get; protected set; }

	public override bool AllowSelfDamage => false;

	public override string RagdollInspectText => string.Format(this._deathReasonFormat, this._ammoName);

	public override string DeathScreenText => string.Empty;

	public override string ServerLogsText => "Shot by " + this.Attacker.Nickname + " with " + this.WeaponType.ToString() + " to the '" + base.Hitbox.ToString() + "' hitbox.";

	public override string ServerMetricsText => base.ServerMetricsText + "," + this.WeaponType;

	public Firearm Firearm { get; private set; }

	public ItemType WeaponType { get; private set; }

	public ItemType AmmoType { get; private set; }

	public FirearmDamageHandler()
	{
		this._deathReasonFormat = DeathTranslations.BulletWounds.RagdollTranslation;
	}

	public FirearmDamageHandler(Firearm firearm, float damage, float penetration, bool useHumanMutlipliers = true)
		: this()
	{
		this.Damage = damage;
		this.Attacker = firearm.Footprint;
		this.Firearm = firearm;
		this.WeaponType = firearm.ItemTypeId;
		this.AmmoType = (firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module) ? module.AmmoType : ItemType.None);
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
		writer.WriteByte((byte)base.Hitbox);
		writer.WriteSByte(this._hitDirectionX);
		writer.WriteSByte(this._hitDirectionZ);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		this.WeaponType = (ItemType)reader.ReadSByte();
		this.AmmoType = (ItemType)reader.ReadSByte();
		base.Hitbox = (HitboxType)reader.ReadByte();
		this._hitDirectionX = reader.ReadSByte();
		this._hitDirectionZ = reader.ReadSByte();
		if (InventoryItemLoader.TryGetItem<AmmoItem>(this.AmmoType, out var result))
		{
			this._ammoName = result.Name;
		}
	}

	public override void ProcessRagdoll(BasicRagdoll ragdoll)
	{
		base.ProcessRagdoll(ragdoll);
		if (!FirearmDamageHandler.HitboxToForce.TryGetValue(base.Hitbox, out var value) || !(ragdoll is DynamicRagdoll dynamicRagdoll))
		{
			return;
		}
		float value2;
		float num = value * (FirearmDamageHandler.AmmoToForce.TryGetValue(this.AmmoType, out value2) ? value2 : 1f);
		Rigidbody[] linkedRigidbodies = dynamicRagdoll.LinkedRigidbodies;
		for (int i = 0; i < linkedRigidbodies.Length; i++)
		{
			linkedRigidbodies[i].AddForce(num * 127f * 0.1f * Vector3.up, ForceMode.VelocityChange);
		}
		Vector3 force = new Vector3(this._hitDirectionX, 0f, this._hitDirectionZ) * num;
		HitboxData[] hitboxes = dynamicRagdoll.Hitboxes;
		for (int i = 0; i < hitboxes.Length; i++)
		{
			HitboxData hitboxData = hitboxes[i];
			if (hitboxData.RelatedHitbox == base.Hitbox)
			{
				hitboxData.Target.AddForce(force, ForceMode.VelocityChange);
			}
		}
	}

	protected override void ProcessDamage(ReferenceHub ply)
	{
		if (!this._useHumanHitboxes && ply.IsHuman())
		{
			base.Hitbox = HitboxType.Body;
		}
		if (this._useHumanHitboxes && FirearmDamageHandler.HitboxDamageMultipliers.TryGetValue(base.Hitbox, out var value))
		{
			this.Damage *= value;
		}
		base.ProcessDamage(ply);
		if (this.Damage != 0f && ply.roleManager.CurrentRole is IArmoredRole armoredRole)
		{
			int armorEfficacy = armoredRole.GetArmorEfficacy(base.Hitbox);
			int bulletPenetrationPercent = Mathf.RoundToInt(this._penetration * 100f);
			float num = Mathf.Clamp(ply.playerStats.GetModule<HumeShieldStat>().CurValue, 0f, this.Damage);
			float baseDamage = Mathf.Max(0f, this.Damage - num);
			float num2 = BodyArmorUtils.ProcessDamage(armorEfficacy, baseDamage, bulletPenetrationPercent);
			this.Damage = num2 + num;
		}
	}
}
