using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Rewards;

public static class TeammateProtectionRewards
{
	private class TrackedTeammate
	{
		public readonly ReferenceHub Hub;

		public readonly FpcStandardRoleBase Role;

		private readonly Dictionary<uint, double> _attackers;

		private const float MinDamage = 100f;

		private const float TimeTolerance = 6f;

		private const int AttackersLimit = 5;

		private double _lastDamageTime;

		private float _damageReceived;

		private static readonly Vector3[] AttackersNonAlloc = new Vector3[5];

		public TrackedTeammate(ReferenceHub ply)
		{
			this.Hub = ply;
			this.Role = ply.roleManager.CurrentRole as FpcStandardRoleBase;
			this._attackers = new Dictionary<uint, double>();
			this.Hub.playerStats.OnThisPlayerDamaged += OnDamaged;
		}

		public void Unsubscribe()
		{
			if (!(this.Hub == null))
			{
				this.Hub.playerStats.OnThisPlayerDamaged -= OnDamaged;
			}
		}

		public int GetAttackersNonAlloc(out Vector3[] attackersPositions)
		{
			attackersPositions = TrackedTeammate.AttackersNonAlloc;
			if (NetworkTime.time > this._lastDamageTime || this._damageReceived < 100f)
			{
				return 0;
			}
			int num = 0;
			foreach (KeyValuePair<uint, double> attacker in this._attackers)
			{
				if (!(attacker.Value > this._lastDamageTime) && ReferenceHub.TryGetHubNetID(attacker.Key, out var hub) && hub.roleManager.CurrentRole is IFpcRole fpcRole)
				{
					TrackedTeammate.AttackersNonAlloc[num] = fpcRole.FpcModule.Position;
					if (++num >= 5)
					{
						break;
					}
				}
			}
			this._attackers.Clear();
			this._damageReceived = 0f;
			return num;
		}

		private void OnDamaged(DamageHandlerBase dhb)
		{
			if (dhb is AttackerDamageHandler attackerDamageHandler && !(dhb is Scp018DamageHandler) && !(dhb is ExplosionDamageHandler))
			{
				double time = NetworkTime.time;
				if (time > this._lastDamageTime)
				{
					this._damageReceived = 0f;
				}
				this._damageReceived += attackerDamageHandler.DealtHealthDamage;
				this._lastDamageTime = time + 6.0;
				this._attackers[attackerDamageHandler.Attacker.NetId] = this._lastDamageTime;
			}
		}
	}

	private const float Cooldown = 10f;

	private static readonly int[] Rewards = new int[6] { 0, 10, 15, 25, 40, 60 };

	private static readonly HashSet<TrackedTeammate> Teammates = new HashSet<TrackedTeammate>();

	private static double _grantTargetCooldown;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Scp079DoorAbility.OnServerAnyDoorInteraction += CheckBlock;
		PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase prev, PlayerRoleBase cur)
		{
			if (NetworkServer.active)
			{
				if (TeammateProtectionRewards.ValidateRole(prev))
				{
					TeammateProtectionRewards.Teammates.RemoveWhere((TrackedTeammate x) => x.Hub == hub);
				}
				if (TeammateProtectionRewards.ValidateRole(cur))
				{
					TeammateProtectionRewards.Teammates.Add(new TrackedTeammate(hub));
				}
			}
		};
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			if (NetworkServer.active && TeammateProtectionRewards.ValidateRole(hub.roleManager.CurrentRole))
			{
				TeammateProtectionRewards.Teammates.RemoveWhere((TrackedTeammate x) => x.Hub == hub);
			}
		};
	}

	private static bool ValidateRole(PlayerRoleBase prb)
	{
		if (prb is FpcStandardRoleBase)
		{
			return prb.Team == Team.SCPs;
		}
		return false;
	}

	private static void CheckBlock(Scp079Role scp079, DoorVariant dv)
	{
		if (dv.TargetState)
		{
			return;
		}
		DoorLockReason activeLocks = (DoorLockReason)dv.ActiveLocks;
		if ((!activeLocks.HasFlagFast(DoorLockReason.Lockdown079) && !activeLocks.HasFlagFast(DoorLockReason.Regular079)) || TeammateProtectionRewards._grantTargetCooldown > NetworkTime.time)
		{
			return;
		}
		int num = 0;
		Transform transform = dv.transform;
		foreach (TrackedTeammate teammate in TeammateProtectionRewards.Teammates)
		{
			Vector3[] attackersPositions;
			int attackersNonAlloc = teammate.GetAttackersNonAlloc(out attackersPositions);
			if (attackersNonAlloc == 0)
			{
				continue;
			}
			bool flag = transform.InverseTransformPoint(teammate.Role.FpcModule.Position).z > 0f;
			for (int i = 0; i < attackersNonAlloc; i++)
			{
				bool flag2 = transform.InverseTransformPoint(attackersPositions[i]).z > 0f;
				if (flag != flag2)
				{
					num++;
				}
			}
		}
		int num2 = Mathf.Min(num, TeammateProtectionRewards.Rewards.Length - 1);
		Scp079RewardManager.GrantExp(scp079, TeammateProtectionRewards.Rewards[num2], Scp079HudTranslation.ExpGainTeammateProtection);
		TeammateProtectionRewards._grantTargetCooldown = NetworkTime.time + 10.0;
	}
}
