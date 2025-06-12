using MapGeneration.Holidays;
using Mirror;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507Role : FpcStandardScp, ISubroutinedRole, IHudScp, IHumeShieldedRole, IHolidayRole
{
	private const float ResurrectHealthMultiplier = 0.5f;

	[SerializeField]
	private Team _team = Team.OtherAlive;

	[SerializeField]
	private Color _roleColor;

	private RoleChangeReason _syncSpawnReason;

	[field: SerializeField]
	public SubroutineManagerModule SubroutineModule { get; private set; }

	[field: SerializeField]
	public HumeShieldModuleBase HumeShieldModule { get; private set; }

	[field: SerializeField]
	public ScpHudBase HudPrefab { get; private set; }

	public bool AlreadyRevived => this.SpawnReason == RoleChangeReason.Revived;

	public HolidayType[] TargetHolidays => new HolidayType[2]
	{
		HolidayType.Christmas,
		HolidayType.AprilFools
	};

	public override ISpawnpointHandler SpawnpointHandler
	{
		get
		{
			if (base.ServerSpawnReason == RoleChangeReason.Revived)
			{
				return null;
			}
			return base.SpawnpointHandler;
		}
	}

	public override float MaxHealth
	{
		get
		{
			float num = (this.AlreadyRevived ? 0.5f : 1f);
			return base.MaxHealth * num;
		}
	}

	public override Team Team => this._team;

	public override Color RoleColor => this._roleColor;

	private RoleChangeReason SpawnReason
	{
		get
		{
			if (!NetworkServer.active)
			{
				return this._syncSpawnReason;
			}
			return base.ServerSpawnReason;
		}
	}

	public override void WritePublicSpawnData(NetworkWriter writer)
	{
		base.WritePublicSpawnData(writer);
		writer.WriteByte((byte)base.ServerSpawnReason);
	}

	public override void ReadSpawnData(NetworkReader reader)
	{
		base.ReadSpawnData(reader);
		this._syncSpawnReason = (RoleChangeReason)reader.ReadByte();
	}
}
