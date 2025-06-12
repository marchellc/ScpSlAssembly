using Footprinting;

namespace PlayerStatsSystem;

public class RecontainmentDamageHandler : AttackerDamageHandler
{
	private readonly string _ragdollinspectText;

	private readonly string _deathscreenText;

	public override Footprint Attacker { get; protected set; }

	public override bool AllowSelfDamage => true;

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._ragdollinspectText;

	public override string DeathScreenText => this._deathscreenText;

	public override string ServerLogsText => "Recontained by " + this.Attacker.Nickname;

	public RecontainmentDamageHandler(Footprint attacker)
	{
		this.Attacker = attacker;
		this.Damage = -1f;
		this._ragdollinspectText = DeathTranslations.Recontained.RagdollTranslation;
		this._deathscreenText = DeathTranslations.Recontained.DeathscreenTranslation;
	}
}
