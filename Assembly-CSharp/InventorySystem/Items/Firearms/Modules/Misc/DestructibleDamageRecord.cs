using PlayerStatsSystem;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public readonly struct DestructibleDamageRecord
{
	public readonly IDestructible Destructible;

	public readonly float AppliedDamage;

	public readonly AttackerDamageHandler Handler;

	public DestructibleDamageRecord(IDestructible destructible, float appliedDamage, AttackerDamageHandler handler)
	{
		this.Destructible = destructible;
		this.AppliedDamage = appliedDamage;
		this.Handler = handler;
	}
}
