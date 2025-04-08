using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Rewards
{
	public static class TeammateProtectionRewards
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Scp079DoorAbility.OnServerAnyDoorInteraction += TeammateProtectionRewards.CheckBlock;
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase prev, PlayerRoleBase cur)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				if (TeammateProtectionRewards.ValidateRole(prev))
				{
					TeammateProtectionRewards.Teammates.RemoveWhere((TeammateProtectionRewards.TrackedTeammate x) => x.Hub == hub);
				}
				if (TeammateProtectionRewards.ValidateRole(cur))
				{
					TeammateProtectionRewards.Teammates.Add(new TeammateProtectionRewards.TrackedTeammate(hub));
				}
			};
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				if (TeammateProtectionRewards.ValidateRole(hub.roleManager.CurrentRole))
				{
					TeammateProtectionRewards.Teammates.RemoveWhere((TeammateProtectionRewards.TrackedTeammate x) => x.Hub == hub);
				}
			}));
		}

		private static bool ValidateRole(PlayerRoleBase prb)
		{
			return prb is FpcStandardRoleBase && prb.Team == Team.SCPs;
		}

		private static void CheckBlock(Scp079Role scp079, DoorVariant dv)
		{
			if (dv.TargetState)
			{
				return;
			}
			DoorLockReason activeLocks = (DoorLockReason)dv.ActiveLocks;
			if (!activeLocks.HasFlagFast(DoorLockReason.Lockdown079) && !activeLocks.HasFlagFast(DoorLockReason.Regular079))
			{
				return;
			}
			if (TeammateProtectionRewards._grantTargetCooldown > NetworkTime.time)
			{
				return;
			}
			int num = 0;
			Transform transform = dv.transform;
			foreach (TeammateProtectionRewards.TrackedTeammate trackedTeammate in TeammateProtectionRewards.Teammates)
			{
				Vector3[] array;
				int attackersNonAlloc = trackedTeammate.GetAttackersNonAlloc(out array);
				if (attackersNonAlloc != 0)
				{
					bool flag = transform.InverseTransformPoint(trackedTeammate.Role.FpcModule.Position).z > 0f;
					for (int i = 0; i < attackersNonAlloc; i++)
					{
						bool flag2 = transform.InverseTransformPoint(array[i]).z > 0f;
						if (flag != flag2)
						{
							num++;
						}
					}
				}
			}
			int num2 = Mathf.Min(num, TeammateProtectionRewards.Rewards.Length - 1);
			Scp079RewardManager.GrantExp(scp079, TeammateProtectionRewards.Rewards[num2], Scp079HudTranslation.ExpGainTeammateProtection, RoleTypeId.None);
			TeammateProtectionRewards._grantTargetCooldown = NetworkTime.time + 10.0;
		}

		private const float Cooldown = 10f;

		private static readonly int[] Rewards = new int[] { 0, 10, 15, 25, 40, 60 };

		private static readonly HashSet<TeammateProtectionRewards.TrackedTeammate> Teammates = new HashSet<TeammateProtectionRewards.TrackedTeammate>();

		private static double _grantTargetCooldown;

		private class TrackedTeammate
		{
			public TrackedTeammate(ReferenceHub ply)
			{
				this.Hub = ply;
				this.Role = ply.roleManager.CurrentRole as FpcStandardRoleBase;
				this._attackers = new Dictionary<uint, double>();
				this.Hub.playerStats.OnThisPlayerDamaged += this.OnDamaged;
			}

			public void Unsubscribe()
			{
				if (this.Hub == null)
				{
					return;
				}
				this.Hub.playerStats.OnThisPlayerDamaged -= this.OnDamaged;
			}

			public int GetAttackersNonAlloc(out Vector3[] attackersPositions)
			{
				attackersPositions = TeammateProtectionRewards.TrackedTeammate.AttackersNonAlloc;
				if (NetworkTime.time > this._lastDamageTime || this._damageReceived < 100f)
				{
					return 0;
				}
				int num = 0;
				foreach (KeyValuePair<uint, double> keyValuePair in this._attackers)
				{
					ReferenceHub referenceHub;
					if (keyValuePair.Value <= this._lastDamageTime && ReferenceHub.TryGetHubNetID(keyValuePair.Key, out referenceHub))
					{
						IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
						if (fpcRole != null)
						{
							TeammateProtectionRewards.TrackedTeammate.AttackersNonAlloc[num] = fpcRole.FpcModule.Position;
							if (++num >= 5)
							{
								break;
							}
						}
					}
				}
				this._attackers.Clear();
				this._damageReceived = 0f;
				return num;
			}

			private void OnDamaged(DamageHandlerBase dhb)
			{
				AttackerDamageHandler attackerDamageHandler = dhb as AttackerDamageHandler;
				if (attackerDamageHandler == null)
				{
					return;
				}
				if (dhb is Scp018DamageHandler || dhb is ExplosionDamageHandler)
				{
					return;
				}
				double time = NetworkTime.time;
				if (time > this._lastDamageTime)
				{
					this._damageReceived = 0f;
				}
				this._damageReceived += attackerDamageHandler.DealtHealthDamage;
				this._lastDamageTime = time + 6.0;
				this._attackers[attackerDamageHandler.Attacker.NetId] = this._lastDamageTime;
			}

			public readonly ReferenceHub Hub;

			public readonly FpcStandardRoleBase Role;

			private readonly Dictionary<uint, double> _attackers;

			private const float MinDamage = 100f;

			private const float TimeTolerance = 6f;

			private const int AttackersLimit = 5;

			private double _lastDamageTime;

			private float _damageReceived;

			private static readonly Vector3[] AttackersNonAlloc = new Vector3[5];
		}
	}
}
