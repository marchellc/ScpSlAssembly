using PlayerRoles.PlayableScps.HumeShield;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173ShieldController : DynamicHumeShieldController
{
	public override float HsRegeneration
	{
		get
		{
			float num = base.HsRegeneration * 0.01f;
			return this.HsMax * num;
		}
	}
}
