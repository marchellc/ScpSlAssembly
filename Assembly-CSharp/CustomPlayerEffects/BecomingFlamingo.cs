using MapGeneration.Holidays;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp1507;

namespace CustomPlayerEffects;

public class BecomingFlamingo : StatusEffectBase, IHolidayEffect
{
	public HolidayType[] TargetHolidays { get; } = new HolidayType[2]
	{
		HolidayType.Christmas,
		HolidayType.AprilFools
	};

	internal override void OnRoleChanged(PlayerRoleBase previousRole, PlayerRoleBase newRole)
	{
		if (newRole is Scp1507Role)
		{
			base.OnRoleChanged(previousRole, newRole);
		}
	}

	internal override void OnDeath(PlayerRoleBase previousRole)
	{
	}
}
