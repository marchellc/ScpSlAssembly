using System;
using InventorySystem;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Rewards
{
	public static class HidStoppedReward
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase x, PlayerRoleBase y)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				if (y is Scp079Role)
				{
					HidStoppedReward._available = true;
				}
			};
			PlayerStats.OnAnyPlayerDamaged += delegate(ReferenceHub hub, DamageHandlerBase dh)
			{
				if (!NetworkServer.active || !HidStoppedReward._available)
				{
					return;
				}
				if (dh is MicroHidDamageHandler)
				{
					HidStoppedReward._microDamageCooldown = NetworkTime.time + 10.0;
				}
			};
			PlayerStats.OnAnyPlayerDied += delegate(ReferenceHub hub, DamageHandlerBase dh)
			{
				if (!NetworkServer.active || !HidStoppedReward._available)
				{
					return;
				}
				MicroHIDItem microHIDItem = hub.inventory.CurInstance as MicroHIDItem;
				if (microHIDItem == null)
				{
					return;
				}
				if (microHIDItem == null || microHIDItem.CycleController.ServerWindUpProgress < 0.75f)
				{
					return;
				}
				HidStoppedReward.TryGrant(hub);
			};
			CycleController.OnPhaseChanged += delegate(ushort serial, MicroHidPhase phase)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				CycleController cycleController = CycleSyncModule.GetCycleController(serial);
				if (!HidStoppedReward._available || NetworkTime.time < HidStoppedReward._microDamageCooldown)
				{
					return;
				}
				if (phase != MicroHidPhase.WindingDown || cycleController.ServerWindUpProgress < 0.75f)
				{
					return;
				}
				ReferenceHub referenceHub;
				if (!InventoryExtensions.TryGetHubHoldingSerial(serial, out referenceHub))
				{
					return;
				}
				HidStoppedReward.TryGrant(referenceHub);
			};
		}

		private static void TryGrant(ReferenceHub ply)
		{
			IFpcRole fpcRole = ply.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			Vector3 humanPos = fpcRole.FpcModule.Position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(humanPos, true);
			if (roomIdentifier == null)
			{
				return;
			}
			if (!ReferenceHub.AllHubs.Any((ReferenceHub x) => HidStoppedReward.IsNearbyTeammate(humanPos, x)))
			{
				return;
			}
			foreach (Scp079Role scp079Role in Scp079Role.ActiveInstances)
			{
				if (Scp079RewardManager.CheckForRoomInteractions(scp079Role, roomIdentifier))
				{
					HidStoppedReward._available = false;
					Scp079RewardManager.GrantExp(scp079Role, 50, Scp079HudTranslation.ExpGainHidStopped, RoleTypeId.None);
				}
			}
		}

		private static bool IsNearbyTeammate(Vector3 attackerPos, ReferenceHub teammate)
		{
			if (!teammate.IsSCP(false))
			{
				return false;
			}
			IFpcRole fpcRole = teammate.roleManager.CurrentRole as IFpcRole;
			return fpcRole != null && (fpcRole.FpcModule.Position - attackerPos).sqrMagnitude < 600f;
		}

		private const int Reward = 50;

		private const float MinReadiness = 0.75f;

		private const float TimeTolerance = 10f;

		private const float ScpMinProximitySqr = 600f;

		private static bool _available;

		private static double _microDamageCooldown;
	}
}
