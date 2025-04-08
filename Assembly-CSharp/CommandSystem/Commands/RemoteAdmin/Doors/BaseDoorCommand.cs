using System;
using System.Linq;
using System.Text;
using Interactables.Interobjects.DoorUtils;
using NorthwoodLib.Pools;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin.Doors
{
	public abstract class BaseDoorCommand : ICommand, IUsageProvider
	{
		public abstract string Command { get; }

		public abstract string[] Aliases { get; }

		public abstract string Description { get; }

		public virtual string[] Usage { get; } = new string[] { "%door%" };

		public virtual bool AllowNonDamageableTargets
		{
			get
			{
				return true;
			}
		}

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			if (arguments.Count == 0)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			if (string.IsNullOrEmpty(arguments.At(0)))
			{
				response = "Please specify a door first.";
				return false;
			}
			bool flag = false;
			string text = arguments.At(0).ToUpper();
			string[] array = text.Split('.', StringSplitOptions.None);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			if (!(text == "*"))
			{
				if (!(text == "!*"))
				{
					if (text == "**")
					{
						stringBuilder.Append(", **");
					}
				}
				else
				{
					stringBuilder.Append(", !*");
				}
			}
			else
			{
				stringBuilder.Append(", *");
			}
			foreach (DoorVariant doorVariant in DoorVariant.AllDoors)
			{
				INonInteractableDoor nonInteractableDoor = doorVariant as INonInteractableDoor;
				if (nonInteractableDoor == null || !nonInteractableDoor.IgnoreRemoteAdmin)
				{
					string text2 = string.Empty;
					DoorNametagExtension nt;
					bool flag2 = doorVariant.TryGetComponent<DoorNametagExtension>(out nt);
					if (!(text == "*"))
					{
						if (!(text == "!*"))
						{
							if (!(text == "**"))
							{
								if (!flag2 || !array.Any((string i) => string.Equals(nt.GetName, i, StringComparison.OrdinalIgnoreCase)))
								{
									continue;
								}
								text2 = nt.GetName;
							}
						}
						else
						{
							if (flag2)
							{
								continue;
							}
							Transform parent = doorVariant.transform.parent;
							DoorNametagExtension doorNametagExtension;
							if (parent != null && parent.TryGetComponent<DoorNametagExtension>(out doorNametagExtension))
							{
								continue;
							}
						}
					}
					else if (!flag2)
					{
						continue;
					}
					if (this.AllowNonDamageableTargets || doorVariant is IDamageableDoor)
					{
						this.OnTargetFound(doorVariant);
						if (!string.IsNullOrEmpty(text2))
						{
							stringBuilder.Append(", " + text2);
						}
						flag = true;
					}
				}
			}
			string text3 = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
			if (flag)
			{
				text3 = text3.Substring(2);
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[] { sender.LogName, " ran ", this.Command, " on the following door(s): ", text3 }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			response = (flag ? ("Affected the following door(s): " + text3) : ("Can't find any door(s) using \"" + text.Replace(".", ", ") + "\"."));
			return flag;
		}

		protected abstract void OnTargetFound(DoorVariant door);
	}
}
