using Mirror;

namespace PlayerRoles.Subroutines;

public class TolerantAbilityCooldown : AbilityCooldown
{
	private readonly double _tolerance;

	public bool TolerantIsReady => NetworkTime.time >= base.NextUse - this._tolerance;

	public TolerantAbilityCooldown(float tolerance = 0.2f)
	{
		this._tolerance = tolerance;
	}

	public override void Trigger(double cooldown)
	{
		if (this.IsReady)
		{
			base.Trigger(cooldown);
		}
		else
		{
			base.NextUse += cooldown;
		}
	}
}
