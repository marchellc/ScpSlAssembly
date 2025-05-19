using System.Text;
using InventorySystem;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Role : FpcStandardScp, IHumeShieldedRole, IInventoryRole, ISubroutinedRole, IHudScp, ICustomNicknameDisplayRole, IDamageHandlerProcessingRole, IHitmarkerPreventer
{
	private Scp3114Identity _identity;

	private Scp3114DamageProcessor _damageProcessor;

	[field: SerializeField]
	public HumeShieldModuleBase HumeShieldModule { get; private set; }

	[field: SerializeField]
	public SubroutineManagerModule SubroutineModule { get; private set; }

	[field: SerializeField]
	public ScpHudBase HudPrefab { get; private set; }

	public Scp3114Identity.StolenIdentity CurIdentity => _identity.CurIdentity;

	public bool Disguised
	{
		get
		{
			return CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active;
		}
		set
		{
			if (value != Disguised && NetworkServer.active)
			{
				CurIdentity.Status = (value ? Scp3114Identity.DisguiseStatus.Active : Scp3114Identity.DisguiseStatus.None);
				_identity.ServerResendIdentity();
			}
		}
	}

	public bool SkeletonIdle => CurIdentity.Status == Scp3114Identity.DisguiseStatus.None;

	public Color NicknameColor => _identity.NicknameColor;

	private void Awake()
	{
		SubroutineModule.TryGetSubroutine<Scp3114Identity>(out _identity);
		SubroutineModule.TryGetSubroutine<Scp3114DamageProcessor>(out _damageProcessor);
	}

	public void WriteNickname(StringBuilder sb)
	{
		_identity.WriteNickname(sb);
	}

	public DamageHandlerBase ProcessDamageHandler(DamageHandlerBase dhb)
	{
		return _damageProcessor.ProcessDamageHandler(dhb);
	}

	public bool TryPreventHitmarker(AttackerDamageHandler adh)
	{
		if (!Disguised)
		{
			return false;
		}
		RoleTypeId role = adh.Attacker.Role;
		RoleTypeId stolenRole = CurIdentity.StolenRole;
		return !HitboxIdentity.IsDamageable(role, stolenRole);
	}

	public bool AllowDisarming(ReferenceHub detainer)
	{
		if (CurIdentity.Status != Scp3114Identity.DisguiseStatus.Active)
		{
			return false;
		}
		if (CurIdentity.StolenRole.GetFaction() == detainer.GetFaction())
		{
			return false;
		}
		return true;
	}

	public bool AllowUndisarming(ReferenceHub releaser)
	{
		return releaser.IsHuman();
	}
}
