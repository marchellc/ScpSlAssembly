using Footprinting;
using InventorySystem.Items.Armor;
using UnityEngine;

namespace PlayerStatsSystem;

public class ExplosionDamageHandler : AttackerDamageHandler
{
	private readonly string _deathScreenText;

	private readonly string _serverLogsText;

	private readonly string _ragdollInspectText;

	private readonly Vector3 _force;

	private const float ForceMultiplier = 1.3f;

	public override float Damage { get; set; }

	public override Footprint Attacker { get; protected set; }

	public override bool AllowSelfDamage => true;

	public override string ServerLogsText => this._serverLogsText;

	public override string RagdollInspectText => this._ragdollInspectText;

	public override string DeathScreenText => this._deathScreenText;

	public ExplosionType ExplosionType { get; private set; }

	public override string ServerMetricsText => base.ServerMetricsText + "," + this.ExplosionType;

	public override HandlerOutput ApplyDamage(ReferenceHub ply)
	{
		HandlerOutput result = base.ApplyDamage(ply);
		base.StartVelocity += this._force * 1.3f;
		return result;
	}

	public ExplosionDamageHandler(Footprint attacker, Vector3 force, float damage, int armorPenetration, ExplosionType explosionType)
	{
		if (armorPenetration != 0)
		{
			this.Attacker = attacker;
			this.ExplosionType = explosionType;
			this._force = force;
			this._serverLogsText = DeathTranslations.Explosion.LogLabel + " caused by " + attacker.Nickname;
			this.Damage = BodyArmorUtils.ProcessDamage((attacker.Hub != null && attacker.Hub.inventory.TryGetBodyArmor(out var bodyArmor)) ? bodyArmor.VestEfficacy : 0, damage, armorPenetration);
		}
	}
}
