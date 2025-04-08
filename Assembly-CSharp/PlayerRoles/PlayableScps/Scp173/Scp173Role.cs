using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173Role : FpcStandardScp, ISubroutinedRole, IArmoredRole, IHumeShieldedRole, IHudScp, ISpawnableScp
	{
		private bool DamagedEventActive
		{
			get
			{
				return this._damagedEventAssigned;
			}
			set
			{
				if (value == this.DamagedEventActive || (value && !NetworkServer.active))
				{
					return;
				}
				PlayerStats playerStats = this._owner.playerStats;
				if (value)
				{
					playerStats.OnThisPlayerDamaged += this.OnDamaged;
				}
				else
				{
					playerStats.OnThisPlayerDamaged -= this.OnDamaged;
				}
				this._damagedEventAssigned = value;
			}
		}

		public ScpDamageHandler DamageHandler
		{
			get
			{
				ReferenceHub referenceHub;
				if (!base.TryGetOwner(out referenceHub))
				{
					throw new InvalidOperationException("Damage handler could not be created for an inactive instance of SCP-173.");
				}
				return new ScpDamageHandler(referenceHub, DeathTranslations.Scp173);
			}
		}

		public HumeShieldModuleBase HumeShieldModule { get; private set; }

		public SubroutineManagerModule SubroutineModule { get; private set; }

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
			if (this.HumeShieldModule.HsCurrent <= 0f)
			{
				return this._armorEfficacy;
			}
			return 0;
		}

		public float GetSpawnChance(List<RoleTypeId> alreadySpawned)
		{
			return 1f;
		}

		[SerializeField]
		private int _armorEfficacy;

		private ReferenceHub _owner;

		private Scp173AudioPlayer _audio;

		private bool _damagedEventAssigned;
	}
}
