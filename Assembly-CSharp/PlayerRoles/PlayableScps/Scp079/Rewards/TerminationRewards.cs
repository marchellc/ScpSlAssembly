using System;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Rewards
{
	public static class TerminationRewards
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerStats.OnAnyPlayerDied += TerminationRewards.OnHumanTerminated;
			Scp106Attack.OnPlayerTeleported += TerminationRewards.OnPlayerTeleported;
		}

		private static void OnPlayerTeleported(ReferenceHub scp106, ReferenceHub hub)
		{
			TerminationRewards.OnHumanTerminated(hub, new ScpDamageHandler(scp106, DeathTranslations.PocketDecay));
		}

		private static void OnHumanTerminated(ReferenceHub ply, DamageHandlerBase damageHandler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Scp079Role.ActiveInstances.ForEach(delegate(Scp079Role x)
			{
				TerminationRewards.GainReward(x, ply, damageHandler);
			});
		}

		private static void GainReward(Scp079Role scp079, ReferenceHub deadPly, DamageHandlerBase damageHandler)
		{
			PlayerRoleBase currentRole = deadPly.roleManager.CurrentRole;
			IFpcRole fpcRole = currentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			int num;
			if (!TerminationRewards.TryGetBaseReward(currentRole.RoleTypeId, out num))
			{
				return;
			}
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(fpcRole.FpcModule.Position, true);
			Scp079HudTranslation scp079HudTranslation = TerminationRewards.EvaluateGainReason(deadPly, damageHandler);
			bool flag = roomIdentifier != null && Scp079RewardManager.CheckForRoomInteractions(scp079, roomIdentifier);
			bool flag2 = scp079.CurrentCamera.Room == roomIdentifier;
			int num2;
			switch (scp079HudTranslation)
			{
			case Scp079HudTranslation.ExpGainTerminationAssist:
				if (flag)
				{
					num2 = num;
					goto IL_00A6;
				}
				scp079HudTranslation = Scp079HudTranslation.ExpGainTerminationWitness;
				break;
			case Scp079HudTranslation.ExpGainTerminationDirect:
				if (!flag)
				{
					return;
				}
				num2 = num * 2;
				goto IL_00A6;
			case Scp079HudTranslation.ExpGainTerminationWitness:
				break;
			default:
				return;
			}
			if (!flag2)
			{
				return;
			}
			num2 = num / 2;
			IL_00A6:
			Scp079RewardManager.GrantExp(scp079, num2, scp079HudTranslation, currentRole.RoleTypeId);
		}

		private static Scp079HudTranslation EvaluateGainReason(ReferenceHub deadPlayer, DamageHandlerBase damageHandler)
		{
			if (TerminationRewards.CheckDirectTermination(damageHandler))
			{
				return Scp079HudTranslation.ExpGainTerminationDirect;
			}
			AttackerDamageHandler attackerDamageHandler = damageHandler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return Scp079HudTranslation.ExpGainTerminationAssist;
			}
			if (attackerDamageHandler.Attacker.Role.GetTeam() == Team.SCPs)
			{
				return Scp079HudTranslation.ExpGainTerminationAssist;
			}
			if (attackerDamageHandler.Attacker.Hub == deadPlayer)
			{
				return Scp079HudTranslation.ExpGainTerminationAssist;
			}
			return Scp079HudTranslation.Zoom;
		}

		private static bool CheckDirectTermination(DamageHandlerBase damageHandler)
		{
			UniversalDamageHandler universalDamageHandler = damageHandler as UniversalDamageHandler;
			return universalDamageHandler != null && universalDamageHandler.TranslationId == DeathTranslations.Tesla.Id;
		}

		public static bool TryGetBaseReward(RoleTypeId rt, out int amount)
		{
			switch (rt.GetTeam())
			{
			case Team.FoundationForces:
				amount = ((rt == RoleTypeId.FacilityGuard) ? 30 : 50);
				return true;
			case Team.ChaosInsurgency:
				amount = 50;
				return true;
			case Team.Scientists:
				amount = 40;
				return true;
			case Team.ClassD:
				amount = 30;
				return true;
			case Team.OtherAlive:
				amount = 50;
				return true;
			case Team.Flamingos:
				amount = 15;
				return true;
			}
			amount = 0;
			return false;
		}
	}
}
