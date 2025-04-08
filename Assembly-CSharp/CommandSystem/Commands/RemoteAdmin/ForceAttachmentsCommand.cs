using System;
using System.Text;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using NorthwoodLib.Pools;
using RemoteAdmin;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ForceAttachmentsCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "forceattachments";

		public string[] Aliases { get; } = new string[] { "forceatt" };

		public string Description { get; } = "Forces certain attachments code on the currently equipped weapon.";

		public string[] Usage { get; } = new string[] { "Attachments Code (Optional)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (!(sender is PlayerCommandSender))
			{
				response = "Only players can run this command.";
				return false;
			}
			Firearm firearm = ((PlayerCommandSender)sender).ReferenceHub.inventory.CurInstance as Firearm;
			if (firearm == null)
			{
				response = "You are not holding any firearm.";
				return false;
			}
			if (arguments.Count == 0)
			{
				StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
				stringBuilder.Append("\nThe attachments combination ID of the currently held firearm is ");
				stringBuilder.Append(firearm.GetCurrentAttachmentsCode());
				stringBuilder.Append("\n\nAttachment codes, based on their order in different slots:\n");
				uint num = 1U;
				for (int i = 0; i < firearm.Attachments.Length; i++)
				{
					stringBuilder.Append(firearm.Attachments[i].Name);
					stringBuilder.Append(" (");
					if (firearm.Attachments[i].IsEnabled)
					{
						stringBuilder.Append("<color=red>");
						stringBuilder.Append(num);
						stringBuilder.Append("</color>");
					}
					else
					{
						stringBuilder.Append(num);
					}
					stringBuilder.Append(")");
					if (i < firearm.Attachments.Length - 1)
					{
						stringBuilder.Append(", ");
						num *= 2U;
					}
				}
				stringBuilder.Append("\n\nRed color indicates currently installed attachments.");
				stringBuilder.Append("\n\nTo change the attachments, use <b><i>forceattachments x</i></b>, where <b><i>x</i></b> is a sum");
				stringBuilder.Append(" of selected attachment codes. The number is later validated, so feel free to experiment - you can't break anything. ");
				stringBuilder.Append("Tip: You can use the plus sign if you don't want to do the math (for example: <i>forceattachments 2+64+1024</i>).");
				response = stringBuilder.ToString();
				StringBuilderPool.Shared.Return(stringBuilder);
				return true;
			}
			string text = RAUtils.FormatArguments(arguments, 0);
			uint num2 = 0U;
			if (text.Contains("+"))
			{
				string[] array = text.Split('+', StringSplitOptions.None);
				for (int j = 0; j < array.Length; j++)
				{
					uint num3;
					if (uint.TryParse(array[j], out num3))
					{
						num2 += num3;
					}
				}
			}
			if (num2 > 0U || uint.TryParse(text, out num2))
			{
				uint num4 = firearm.ValidateAttachmentsCode(num2);
				firearm.ApplyAttachmentsCode(num4, false);
				firearm.ServerResendAttachmentCode();
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} forced attachments code {1} {2} for weapon {3}.", new object[]
				{
					sender.LogName,
					num2,
					(num4 == num2) ? "" : string.Format("(validated: {0})", num4),
					Enum.GetName(typeof(ItemType), firearm.ItemTypeId)
				}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				response = ((num4 == num2) ? ("Successfully assigned code: " + num2.ToString()) : string.Concat(new string[]
				{
					"Successfully assigned code: ",
					num2.ToString(),
					" (validated: ",
					num4.ToString(),
					")"
				}));
				return true;
			}
			response = "Could not parse the code: '" + text + "'";
			return false;
		}
	}
}
