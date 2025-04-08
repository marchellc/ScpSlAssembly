using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp1507;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049
{
	public class Scp049ResurrectAbility : RagdollAbilityBase<Scp049Role>
	{
		protected override float RangeSqr
		{
			get
			{
				return 3.5f;
			}
		}

		protected override float Duration
		{
			get
			{
				return 7f;
			}
		}

		private static int GroundMask
		{
			get
			{
				if (!Scp049ResurrectAbility._maskSet)
				{
					Scp049ResurrectAbility._mask = LayerMask.GetMask(new string[] { "Default" });
					Scp049ResurrectAbility._maskSet = true;
				}
				return Scp049ResurrectAbility._mask;
			}
		}

		protected override void ServerComplete()
		{
			ReferenceHub referenceHub = base.CurRagdoll.Info.OwnerHub;
			Scp049ResurrectingBodyEventArgs scp049ResurrectingBodyEventArgs = new Scp049ResurrectingBodyEventArgs(Ragdoll.Get(base.CurRagdoll), referenceHub, base.Owner);
			Scp049Events.OnResurrectingBody(scp049ResurrectingBodyEventArgs);
			if (!scp049ResurrectingBodyEventArgs.IsAllowed)
			{
				return;
			}
			referenceHub = scp049ResurrectingBodyEventArgs.Target.ReferenceHub;
			referenceHub.transform.position = base.CastRole.FpcModule.Position;
			if (this._senseAbility.DeadTargets.Contains(referenceHub))
			{
				HumeShieldModuleBase humeShieldModule = base.CastRole.HumeShieldModule;
				humeShieldModule.HsCurrent = Mathf.Min(humeShieldModule.HsCurrent + 200f, humeShieldModule.HsMax);
			}
			RoleTypeId roleTypeId = RoleTypeId.Scp0492;
			if (base.CurRagdoll is Scp1507Ragdoll)
			{
				roleTypeId = RoleTypeId.ZombieFlamingo;
			}
			referenceHub.roleManager.ServerSetRole(roleTypeId, RoleChangeReason.Revived, RoleSpawnFlags.All);
			if (!Physics.Raycast(referenceHub.transform.position, Vector3.down, 1f, Scp049ResurrectAbility.GroundMask))
			{
				referenceHub.TryOverridePosition(base.CastRole.transform.position);
			}
			NetworkServer.Destroy(base.CurRagdoll.gameObject);
			Scp049Events.OnResurrectedBody(new Scp049ResurrectedBodyEventArgs(referenceHub, base.Owner));
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp049SenseAbility>(out this._senseAbility);
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			base.ClientTryStart();
		}

		protected override void OnKeyUp()
		{
			base.OnKeyUp();
			base.ClientTryCancel();
		}

		public bool CheckRagdoll(BasicRagdoll ragdoll)
		{
			return this.CheckBeginConditions(ragdoll) == Scp049ResurrectAbility.ResurrectError.None;
		}

		protected override byte ServerValidateBegin(BasicRagdoll ragdoll)
		{
			Scp049ResurrectAbility.ResurrectError resurrectError = this.CheckBeginConditions(ragdoll);
			if (!this.ServerValidateAny())
			{
				resurrectError = Scp049ResurrectAbility.ResurrectError.TargetNull;
			}
			Scp049StartingResurrectionEventArgs scp049StartingResurrectionEventArgs = new Scp049StartingResurrectionEventArgs(resurrectError == Scp049ResurrectAbility.ResurrectError.None, ragdoll, ragdoll.Info.OwnerHub, base.Owner);
			Scp049Events.OnStartingResurrection(scp049StartingResurrectionEventArgs);
			if (!scp049StartingResurrectionEventArgs.IsAllowed)
			{
				return 1;
			}
			resurrectError = (scp049StartingResurrectionEventArgs.CanResurrect ? Scp049ResurrectAbility.ResurrectError.None : Scp049ResurrectAbility.ResurrectError.TargetInvalid);
			return (byte)resurrectError;
		}

		protected override bool ServerValidateAny()
		{
			if (base.CurRagdoll == null)
			{
				return false;
			}
			ReferenceHub ownerHub = base.CurRagdoll.Info.OwnerHub;
			return ownerHub != null && base.ServerValidateAny() && Scp049ResurrectAbility.IsSpawnableSpectator(ownerHub) && this.CheckMaxResurrections(ownerHub) == Scp049ResurrectAbility.ResurrectError.None && !this.AnyConflicts(base.CurRagdoll);
		}

		private Scp049ResurrectAbility.ResurrectError CheckBeginConditions(BasicRagdoll ragdoll)
		{
			ReferenceHub ownerHub = ragdoll.Info.OwnerHub;
			bool flag = ownerHub == null;
			if (ragdoll.Info.RoleType == RoleTypeId.Scp0492)
			{
				if (flag || !Scp049ResurrectAbility.DeadZombies.Contains(ownerHub.netId))
				{
					return Scp049ResurrectAbility.ResurrectError.TargetNull;
				}
				if (!(ragdoll.Info.Handler is AttackerDamageHandler))
				{
					return Scp049ResurrectAbility.ResurrectError.TargetInvalid;
				}
			}
			else
			{
				float num = ((!flag && this._senseAbility.DeadTargets.Contains(ownerHub)) ? 18f : 12f);
				if (ragdoll.Info.ExistenceTime > num)
				{
					return Scp049ResurrectAbility.ResurrectError.Expired;
				}
				if (!Scp049ResurrectAbility.IsResurrectableRole(ragdoll.Info.RoleType))
				{
					return Scp049ResurrectAbility.ResurrectError.TargetInvalid;
				}
				if (flag || this.AnyConflicts(ragdoll))
				{
					return Scp049ResurrectAbility.ResurrectError.TargetNull;
				}
				if (!Scp049ResurrectAbility.IsSpawnableSpectator(ownerHub))
				{
					return Scp049ResurrectAbility.ResurrectError.TargetInvalid;
				}
			}
			return this.CheckMaxResurrections(ownerHub);
		}

		private Scp049ResurrectAbility.ResurrectError CheckMaxResurrections(ReferenceHub owner)
		{
			int resurrectionsNumber = Scp049ResurrectAbility.GetResurrectionsNumber(owner);
			if (resurrectionsNumber < 2)
			{
				return Scp049ResurrectAbility.ResurrectError.None;
			}
			if (resurrectionsNumber <= 2)
			{
				return Scp049ResurrectAbility.ResurrectError.MaxReached;
			}
			return Scp049ResurrectAbility.ResurrectError.Refused;
		}

		private bool AnyConflicts(BasicRagdoll ragdoll)
		{
			Scp3114DamageHandler scp3114DamageHandler = ragdoll.Info.Handler as Scp3114DamageHandler;
			if (scp3114DamageHandler != null && scp3114DamageHandler.Subtype == Scp3114DamageHandler.HandlerType.SkinSteal)
			{
				return true;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Scp3114Role scp3114Role = referenceHub.roleManager.CurrentRole as Scp3114Role;
				if (scp3114Role != null && !scp3114Role.SkeletonIdle && !(scp3114Role.CurIdentity.Ragdoll != ragdoll))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsSpawnableSpectator(ReferenceHub hub)
		{
			SpectatorRole spectatorRole = hub.roleManager.CurrentRole as SpectatorRole;
			return spectatorRole != null && spectatorRole.ReadyToRespawn;
		}

		private static bool IsResurrectableRole(RoleTypeId role)
		{
			return role.IsFlamingo(true) || role.IsHuman();
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnServerRoleSet += delegate(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason changeReason)
			{
				if (newRole != RoleTypeId.Overwatch && !newRole.IsHuman())
				{
					return;
				}
				Scp049ResurrectAbility.ClearPlayerResurrections(hub);
			};
			CustomNetworkManager.OnClientReady += Scp049ResurrectAbility.DeadZombies.Clear;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				Scp049ResurrectAbility.DeadZombies.Remove(hub.netId);
			}));
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				if (prevRole is ZombieRole && newRole is SpectatorRole)
				{
					Scp049ResurrectAbility.DeadZombies.Add(hub.netId);
					return;
				}
				Scp049ResurrectAbility.DeadZombies.Remove(hub.netId);
			};
		}

		public static int GetResurrectionsNumber(ReferenceHub hub)
		{
			int num;
			if (!Scp049ResurrectAbility.ResurrectedPlayers.TryGetValue(hub.netId, out num))
			{
				return 0;
			}
			return num;
		}

		public static void RegisterPlayerResurrection(ReferenceHub hub, int amount = 1)
		{
			Scp049ResurrectAbility.ResurrectedPlayers[hub.netId] = Scp049ResurrectAbility.GetResurrectionsNumber(hub) + amount;
		}

		public static void ClearPlayerResurrections(ReferenceHub hub)
		{
			Scp049ResurrectAbility.ResurrectedPlayers.Remove(hub.netId);
		}

		public const int MaxResurrections = 2;

		private const float TargetCorpseDuration = 18f;

		private const float HumanCorpseDuration = 12f;

		private const float ResurrectTargetReward = 200f;

		private const float MaxRespawnGroundDistance = 1f;

		private static readonly Dictionary<uint, int> ResurrectedPlayers = new Dictionary<uint, int>();

		private static readonly HashSet<uint> DeadZombies = new HashSet<uint>();

		private static bool _maskSet;

		private static int _mask;

		private Scp049SenseAbility _senseAbility;

		[SerializeField]
		private AudioSource _surgerySource;

		private enum ResurrectError
		{
			None,
			TargetNull,
			Expired,
			MaxReached,
			Refused,
			TargetInvalid
		}
	}
}
