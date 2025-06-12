using System.Collections.Generic;
using GameObjectPools;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106Role : FpcStandardScp, ISubroutinedRole, IHudScp, IPoolResettable, IHumeShieldedRole, IDamageHandlerProcessingRole, ITeslaControllerRole, ISpawnableScp
{
	public static readonly HashSet<Scp106Role> AllInstances = new HashSet<Scp106Role>();

	private Scp106SinkholeController _sinkholeCtrl;

	private bool _sinkholeSet;

	[field: SerializeField]
	public HumeShieldModuleBase HumeShieldModule { get; private set; }

	[field: SerializeField]
	public SubroutineManagerModule SubroutineModule { get; private set; }

	[field: SerializeField]
	public ScpHudBase HudPrefab { get; private set; }

	[field: SerializeField]
	public AudioClip ItemSpawnSound { get; private set; }

	public Scp106SinkholeController Sinkhole
	{
		get
		{
			if (!this._sinkholeSet)
			{
				this.SubroutineModule.TryGetSubroutine<Scp106SinkholeController>(out this._sinkholeCtrl);
				this._sinkholeSet = true;
			}
			return this._sinkholeCtrl;
		}
	}

	public bool CanActivateShock => !this.Sinkhole.TargetSubmerged;

	public bool IsStalking
	{
		get
		{
			if (this.SubroutineModule.TryGetSubroutine<Scp106StalkAbility>(out var subroutine))
			{
				return subroutine.StalkActive;
			}
			return false;
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		Scp106Role.AllInstances.Add(this);
	}

	public void ResetObject()
	{
		Scp106Role.AllInstances.Remove(this);
	}

	public DamageHandlerBase ProcessDamageHandler(DamageHandlerBase dhb)
	{
		if (!this._sinkholeCtrl.IsHidden)
		{
			return dhb;
		}
		if (!this.ValidateDamageHandler(dhb))
		{
			dhb = new UniversalDamageHandler();
		}
		return dhb;
	}

	public float GetSpawnChance(List<RoleTypeId> alreadySpawned)
	{
		return 1f;
	}

	public bool IsInIdleRange(TeslaGate teslaGate)
	{
		if (!this.IsStalking)
		{
			return teslaGate.IsInIdleRange(base.FpcModule.Position);
		}
		return false;
	}

	private bool ValidateDamageHandler(DamageHandlerBase dhb)
	{
		if (dhb is UniversalDamageHandler universalDamageHandler && universalDamageHandler.TranslationId == DeathTranslations.Tesla.Id)
		{
			return false;
		}
		if (dhb is ExplosionDamageHandler || dhb is MicroHidDamageHandler || dhb is FirearmDamageHandler || (dhb is Scp018DamageHandler && this.IsStalking))
		{
			return false;
		}
		return true;
	}
}
