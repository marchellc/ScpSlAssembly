using System;
using System.Collections.Generic;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp049
{
	public class Scp049Role : FpcStandardScp, ISubroutinedRole, IHumeShieldedRole, IHudScp, ISpawnableScp
	{
		public HumeShieldModuleBase HumeShieldModule { get; private set; }

		public SubroutineManagerModule SubroutineModule { get; private set; }

		public ScpHudBase HudPrefab { get; private set; }

		public float GetSpawnChance(List<RoleTypeId> alreadySpawned)
		{
			return 1f;
		}
	}
}
