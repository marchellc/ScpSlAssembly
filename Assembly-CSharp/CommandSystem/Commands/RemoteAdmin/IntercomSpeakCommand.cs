using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using PlayerRoles.Voice;
using RemoteAdmin;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class IntercomSpeakCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "icom";

		public string[] Aliases { get; } = new string[] { "speak" };

		public string Description { get; } = "Toggles global voice over the intercom.";

		public string[] Usage { get; } = new string[] { "%player%", "enable/disable" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Broadcasting, out response))
			{
				return false;
			}
			if (arguments.Count == 0)
			{
				PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
				if (playerCommandSender == null)
				{
					response = "You must be in-game to use this command!";
					return false;
				}
				bool flag = !Intercom.HasOverride(playerCommandSender.ReferenceHub);
				if (!Intercom.TrySetOverride(playerCommandSender.ReferenceHub, flag))
				{
					response = "Failed to set override flags. User or intercom is null.";
					return false;
				}
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " toggled global intercom transmission.", ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
				response = "Done! Global voice over the intercom toggled " + (flag ? "on" : "off") + ".";
				return true;
			}
			else
			{
				string[] array;
				List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
				StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
				response = "Invalid argument, state was not defined.";
				if (array.Length == 0)
				{
					return false;
				}
				string text = array[0].ToUpper();
				bool flag2 = false;
				bool flag3 = false;
				uint num = <PrivateImplementationDetails>.ComputeStringHash(text);
				if (num <= 890022063U)
				{
					if (num <= 785938062U)
					{
						if (num != 304993157U)
						{
							if (num != 785938062U)
							{
								goto IL_01CD;
							}
							if (!(text == "ENABLE"))
							{
								goto IL_01CD;
							}
						}
						else
						{
							if (!(text == "DISABLED"))
							{
								goto IL_01CD;
							}
							goto IL_01C8;
						}
					}
					else if (num != 873244444U)
					{
						if (num != 890022063U)
						{
							goto IL_01CD;
						}
						if (!(text == "0"))
						{
							goto IL_01CD;
						}
						goto IL_01C8;
					}
					else if (!(text == "1"))
					{
						goto IL_01CD;
					}
				}
				else if (num <= 2294480894U)
				{
					if (num != 1343949093U)
					{
						if (num != 2294480894U)
						{
							goto IL_01CD;
						}
						if (!(text == "ENABLED"))
						{
							goto IL_01CD;
						}
					}
					else if (!(text == "TRUE"))
					{
						goto IL_01CD;
					}
				}
				else if (num != 2616878531U)
				{
					if (num != 3998840952U)
					{
						goto IL_01CD;
					}
					if (!(text == "FALSE"))
					{
						goto IL_01CD;
					}
					goto IL_01C8;
				}
				else
				{
					if (!(text == "DISABLE"))
					{
						goto IL_01CD;
					}
					goto IL_01C8;
				}
				flag3 = true;
				goto IL_01D0;
				IL_01C8:
				flag3 = false;
				goto IL_01D0;
				IL_01CD:
				flag2 = true;
				IL_01D0:
				int num2 = 0;
				foreach (ReferenceHub referenceHub in list)
				{
					if (flag2)
					{
						flag3 = !Intercom.HasOverride(referenceHub);
					}
					if (Intercom.TrySetOverride(referenceHub, flag3))
					{
						if (num2 != 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
						num2++;
					}
				}
				if (num2 > 0)
				{
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} {1} global intercom transmission for player{2}{3}.", new object[]
					{
						sender.LogName,
						flag3 ? "enabled" : "disabled",
						(num2 == 1) ? " " : "s ",
						stringBuilder
					}), ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
				}
				StringBuilderPool.Shared.Return(stringBuilder);
				response = string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!");
				return true;
			}
		}
	}
}
