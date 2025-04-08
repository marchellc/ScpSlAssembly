using System;
using System.Text;
using InventorySystem;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Role : FpcStandardScp, IHumeShieldedRole, IInventoryRole, ISubroutinedRole, IHudScp, ICustomNicknameDisplayRole, IDamageHandlerProcessingRole, IHitmarkerPreventer
	{
		public HumeShieldModuleBase HumeShieldModule { get; private set; }

		public SubroutineManagerModule SubroutineModule { get; private set; }

		public ScpHudBase HudPrefab { get; private set; }

		public Scp3114Identity.StolenIdentity CurIdentity
		{
			get
			{
				return this._identity.CurIdentity;
			}
		}

		public bool Disguised
		{
			get
			{
				return this.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active;
			}
			set
			{
				if (value == this.Disguised || !NetworkServer.active)
				{
					return;
				}
				this.CurIdentity.Status = (value ? Scp3114Identity.DisguiseStatus.Active : Scp3114Identity.DisguiseStatus.None);
				this._identity.ServerResendIdentity();
			}
		}

		public bool SkeletonIdle
		{
			get
			{
				return this.CurIdentity.Status == Scp3114Identity.DisguiseStatus.None;
			}
		}

		private void Awake()
		{
			this.SubroutineModule.TryGetSubroutine<Scp3114Identity>(out this._identity);
			this.SubroutineModule.TryGetSubroutine<Scp3114DamageProcessor>(out this._damageProcessor);
		}

		public void WriteNickname(ReferenceHub owner, StringBuilder sb, out Color texColor)
		{
			this._identity.WriteNickname(owner, sb, out texColor);
		}

		public DamageHandlerBase ProcessDamageHandler(DamageHandlerBase dhb)
		{
			return this._damageProcessor.ProcessDamageHandler(dhb);
		}

		public bool TryPreventHitmarker(AttackerDamageHandler adh)
		{
			if (!this.Disguised)
			{
				return false;
			}
			RoleTypeId role = adh.Attacker.Role;
			RoleTypeId stolenRole = this.CurIdentity.StolenRole;
			return !HitboxIdentity.IsDamageable(role, stolenRole);
		}

		public bool AllowDisarming(ReferenceHub detainer)
		{
			return this.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active && this.CurIdentity.StolenRole.GetFaction() != detainer.GetFaction();
		}

		public bool AllowUndisarming(ReferenceHub releaser)
		{
			return releaser.IsHuman();
		}

		private Scp3114Identity _identity;

		private Scp3114DamageProcessor _damageProcessor;
	}
}
