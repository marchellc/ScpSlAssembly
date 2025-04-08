using System;
using System.Collections.Generic;
using PlayerStatsSystem;

namespace InventorySystem.Items.Usables
{
	public class PlayerHandler
	{
		public void DoUpdate(ReferenceHub hub)
		{
			HealthStat module = hub.playerStats.GetModule<HealthStat>();
			foreach (RegenerationProcess regenerationProcess in this.ActiveRegenerations)
			{
				bool flag;
				int num;
				regenerationProcess.GetValue(out flag, out num);
				module.ServerHeal((float)num);
				if (flag)
				{
					this.ActiveRegenerations.Remove(regenerationProcess);
					break;
				}
			}
		}

		public void ResetAll()
		{
			this.CurrentUsable = CurrentlyUsedItem.None;
			this.ActiveRegenerations.Clear();
			this.PersonalCooldowns.Clear();
		}

		public CurrentlyUsedItem CurrentUsable = CurrentlyUsedItem.None;

		public readonly List<RegenerationProcess> ActiveRegenerations = new List<RegenerationProcess>();

		public readonly Dictionary<ItemType, float> PersonalCooldowns = new Dictionary<ItemType, float>();
	}
}
