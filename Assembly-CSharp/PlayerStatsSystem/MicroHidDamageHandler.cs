using Footprinting;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
using Mirror;

namespace PlayerStatsSystem;

public class MicroHidDamageHandler : AttackerDamageHandler, DisintegrateDeathAnimation.IDisintegrateDamageHandler
{
	private readonly string _deathScreenText;

	private readonly string _serverLogsText;

	private readonly string _ragdollInspectText;

	public override float Damage { get; set; }

	public override Footprint Attacker { get; protected set; }

	public override bool AllowSelfDamage => this.Disintegrate;

	public override string ServerLogsText => this._serverLogsText;

	public override string RagdollInspectText => this._ragdollInspectText;

	public override string DeathScreenText => this._deathScreenText;

	public MicroHidFiringMode FiringMode { get; private set; }

	public bool Disintegrate { get; private set; }

	public MicroHidDamageHandler(FiringModeControllerModule module, float impulseDamage)
	{
		this._ragdollInspectText = DeathTranslations.MicroHID.RagdollTranslation;
		this._deathScreenText = DeathTranslations.MicroHID.DeathscreenTranslation;
		if (!(module == null))
		{
			this.FiringMode = module.AssignedMode;
			this.Attacker = new Footprint(module.Item.Owner);
			this._serverLogsText = "Deep fried by " + this.Attacker.Nickname + " with " + DeathTranslations.MicroHID.LogLabel + " using " + this.FiringMode;
			this.Damage = impulseDamage;
		}
	}

	public MicroHidDamageHandler(float damage, MicroHIDItem micro)
	{
		this.Attacker = new Footprint(micro.Owner);
		this._serverLogsText = "MicroHID overcharge";
		this.Damage = damage;
		this.Disintegrate = true;
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteByte((byte)this.FiringMode);
		writer.WriteBool(this.Disintegrate);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		this.FiringMode = (MicroHidFiringMode)reader.ReadByte();
		this.Disintegrate = reader.ReadBool();
	}
}
