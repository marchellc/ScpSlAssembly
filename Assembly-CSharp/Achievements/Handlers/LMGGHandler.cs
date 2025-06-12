using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers;

public class LMGGHandler : AchievementHandlerBase
{
	private const int KillsNeeded = 3;

	private static readonly Dictionary<ReferenceHub, double> LastHeldTime = new Dictionary<ReferenceHub, double>();

	private static readonly Dictionary<ReferenceHub, int> KillsSinceHeld = new Dictionary<ReferenceHub, int>();

	internal override void OnInitialize()
	{
		PlayerStats.OnAnyPlayerDied += OnAnyPlayerDied;
	}

	internal override void OnRoundStarted()
	{
		LMGGHandler.LastHeldTime.Clear();
		LMGGHandler.KillsSinceHeld.Clear();
	}

	private void OnAnyPlayerDied(ReferenceHub victim, DamageHandlerBase handler)
	{
		if (!NetworkServer.active || !(handler is FirearmDamageHandler { WeaponType: var weaponType } firearmDamageHandler) || (weaponType != ItemType.GunFRMG0 && weaponType != ItemType.GunLogicer))
		{
			return;
		}
		ReferenceHub hub = firearmDamageHandler.Attacker.Hub;
		if (!hub.IsHuman() || !HitboxIdentity.IsEnemy(firearmDamageHandler.Attacker.Role, victim.GetRoleId()))
		{
			return;
		}
		SimpleTriggerModule.ReceivedData data = SimpleTriggerModule.GetData(firearmDamageHandler.Firearm.ItemSerial);
		if (!LMGGHandler.LastHeldTime.TryGetValue(hub, out var value))
		{
			value = data.PressTime;
			LMGGHandler.LastHeldTime[hub] = value;
		}
		if (Math.Abs(data.PressTime - value) > 1E-09)
		{
			LMGGHandler.LastHeldTime[hub] = data.PressTime;
			LMGGHandler.KillsSinceHeld[hub] = 1;
			return;
		}
		if (!LMGGHandler.KillsSinceHeld.TryGetValue(hub, out var value2))
		{
			value2 = 0;
		}
		value2 = (LMGGHandler.KillsSinceHeld[hub] = value2 + 1);
		if (value2 >= 3)
		{
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.LMGG);
		}
	}
}
