using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;

namespace CustomPlayerEffects;

public class Corroding : TickingEffectBase, IStaminaModifier
{
	private const float DamagePerTick = 2.1f;

	private static readonly float StaminaDrainPercentage;

	public ReferenceHub AttackerHub;

	public override bool AllowEnabling => true;

	public bool StaminaModifierActive => base.IsEnabled;

	public float StaminaRegenMultiplier => 0f;

	protected override void OnTick()
	{
		if (NetworkServer.active && !(this.AttackerHub == null) && !Vitality.CheckPlayer(base.Hub))
		{
			base.Hub.playerStats.DealDamage(new ScpDamageHandler(this.AttackerHub, 2.1f, DeathTranslations.PocketDecay));
			if (Corroding.StaminaDrainPercentage > 0f)
			{
				base.Hub.playerStats.GetModule<StaminaStat>().CurValue -= Corroding.StaminaDrainPercentage * 0.01f;
			}
		}
	}
}
