using System;
using System.Collections.Generic;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096Role : FpcStandardScp, ISubroutinedRole, IHumeShieldedRole, IHudScp, ISpawnableScp
	{
		public Scp096StateController StateController { get; private set; }

		public HumeShieldModuleBase HumeShieldModule { get; private set; }

		public SubroutineManagerModule SubroutineModule { get; private set; }

		public ScpHudBase HudPrefab { get; private set; }

		public float GetSpawnChance(List<RoleTypeId> alreadySpawned)
		{
			return (float)((alreadySpawned.Count == 0 || alreadySpawned.Contains(RoleTypeId.Scp079)) ? 0 : 1);
		}
	}
}
