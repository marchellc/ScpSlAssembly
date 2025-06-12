using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173Role : FpcStandardScp, ISubroutinedRole, IArmoredRole, IHumeShieldedRole, IHudScp, ISpawnableScp
{
	[SerializeField]
	private int _armorEfficacy;

	private ReferenceHub _owner;

	private Scp173AudioPlayer _audio;

	private bool _damagedEventAssigned;

	private bool DamagedEventActive
	{
		get
		{
			return this._damagedEventAssigned;
		}
		set
		{
			if (value != this.DamagedEventActive && (!value || NetworkServer.active))
			{
				PlayerStats playerStats = this._owner.playerStats;
				if (value)
				{
					playerStats.OnThisPlayerDamaged += OnDamaged;
				}
				else
				{
					playerStats.OnThisPlayerDamaged -= OnDamaged;
				}
				this._damagedEventAssigned = value;
			}
		}
	}

	public ScpDamageHandler DamageHandler
	{
		get
		{
			if (!base.TryGetOwner(out var hub))
			{
				throw new InvalidOperationException("Damage handler could not be created for an inactive instance of SCP-173.");
			}
			return new ScpDamageHandler(hub, DeathTranslations.Scp173);
		}
	}

	[field: SerializeField]
	public HumeShieldModuleBase HumeShieldModule { get; private set; }

	[field: SerializeField]
	public SubroutineManagerModule SubroutineModule { get; private set; }

	[field: SerializeField]
	public ScpHudBase HudPrefab { get; private set; }

	private void OnDamaged(DamageHandlerBase obj)
	{
		if (obj is FirearmDamageHandler)
		{
			this._audio.ServerSendSound(Scp173AudioPlayer.Scp173SoundId.Hit);
		}
	}

	private void Awake()
	{
		this.SubroutineModule.TryGetSubroutine<Scp173AudioPlayer>(out this._audio);
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		base.TryGetOwner(out this._owner);
		this.DamagedEventActive = true;
	}

	public override void DisableRole(RoleTypeId newRole)
	{
		base.DisableRole(newRole);
		this.DamagedEventActive = false;
	}

	public int GetArmorEfficacy(HitboxType hitbox)
	{
		return this._armorEfficacy;
	}

	public float GetSpawnChance(List<RoleTypeId> alreadySpawned)
	{
		return 1f;
	}
}
