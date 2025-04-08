using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class LMGGHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += this.OnAnyPlayerDied;
		}

		internal override void OnRoundStarted()
		{
			LMGGHandler.LastHeldTime.Clear();
			LMGGHandler.KillsSinceHeld.Clear();
		}

		private void OnAnyPlayerDied(ReferenceHub victim, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			FirearmDamageHandler firearmDamageHandler = handler as FirearmDamageHandler;
			if (firearmDamageHandler == null)
			{
				return;
			}
			ItemType weaponType = firearmDamageHandler.WeaponType;
			if (weaponType != ItemType.GunFRMG0 && weaponType != ItemType.GunLogicer)
			{
				return;
			}
			ReferenceHub hub = firearmDamageHandler.Attacker.Hub;
			if (!hub.IsHuman())
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(firearmDamageHandler.Attacker.Role, victim.GetRoleId()))
			{
				return;
			}
			SimpleTriggerModule.ReceivedData data = SimpleTriggerModule.GetData(firearmDamageHandler.Firearm.ItemSerial);
			double pressTime;
			if (!LMGGHandler.LastHeldTime.TryGetValue(hub, out pressTime))
			{
				pressTime = data.PressTime;
				LMGGHandler.LastHeldTime[hub] = pressTime;
			}
			if (Math.Abs(data.PressTime - pressTime) > 1E-09)
			{
				LMGGHandler.LastHeldTime[hub] = data.PressTime;
				LMGGHandler.KillsSinceHeld[hub] = 1;
				return;
			}
			int num;
			if (!LMGGHandler.KillsSinceHeld.TryGetValue(hub, out num))
			{
				num = 0;
			}
			num = (LMGGHandler.KillsSinceHeld[hub] = num + 1);
			if (num < 3)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.LMGG);
		}

		private const int KillsNeeded = 3;

		private static readonly Dictionary<ReferenceHub, double> LastHeldTime = new Dictionary<ReferenceHub, double>();

		private static readonly Dictionary<ReferenceHub, int> KillsSinceHeld = new Dictionary<ReferenceHub, int>();
	}
}
