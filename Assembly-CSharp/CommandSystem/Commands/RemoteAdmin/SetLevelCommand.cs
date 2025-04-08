using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using PlayerRoles.PlayableScps.Scp079;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class SetLevelCommand : Scp079CommandBase
	{
		public override string Command { get; } = "setlevel";

		public override string[] Aliases { get; } = new string[] { "settier", "level", "lvl" };

		public override string Description { get; } = "Sets the level of the player playing as SCP-079.";

		public override string[] Usage { get; } = new string[] { "%player%", "New Level" };

		public override bool OnExecute(ArraySegment<string> arguments, ICommandSender sender, int input, out string response)
		{
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num = 0;
			int num2 = 0;
			bool flag = input-- <= 1;
			input--;
			foreach (ReferenceHub referenceHub in list)
			{
				Scp079Role scp079Role = referenceHub.roleManager.CurrentRole as Scp079Role;
				Scp079TierManager scp079TierManager;
				if (scp079Role != null && scp079Role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out scp079TierManager))
				{
					if (!flag)
					{
						num = scp079TierManager.AbsoluteThresholds[Mathf.Clamp(input, 0, scp079TierManager.AbsoluteThresholds.Length - 1)];
					}
					this.ApplyChanges(scp079TierManager, num);
					num2++;
					stringBuilder.Append(", " + referenceHub.LoggedNameFromRefHub());
				}
			}
			if (num2 > 0)
			{
				string text = StringBuilderPool.Shared.ToStringReturn(stringBuilder).Substring(2);
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} used \"{1} ({2})\" command on player{3}{4}.", new object[]
				{
					sender.LogName,
					this.Command,
					input,
					(num2 == 1) ? " " : "s ",
					text
				}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			else
			{
				StringBuilderPool.Shared.Return(stringBuilder);
			}
			response = string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!");
			return true;
		}

		public override void ApplyChanges(Scp079TierManager manager, int exp)
		{
			manager.TotalExp = exp;
		}
	}
}
