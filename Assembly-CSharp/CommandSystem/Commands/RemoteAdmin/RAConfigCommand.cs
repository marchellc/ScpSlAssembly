using System;
using System.Globalization;
using CustomPlayerEffects;
using Utils;
using Utils.CommandInterpolation;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class RAConfigCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "setconfig";

		public string[] Aliases { get; } = new string[] { "sc" };

		public string Description { get; } = "Sets the server configuration.";

		public string[] Usage { get; } = new string[] { "Option", "Value" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.ServerConfigs, out response))
			{
				return false;
			}
			if (arguments.Count < 2)
			{
				response = "To execute this command provide at least 2 arguments!";
				return false;
			}
			string text = RAUtils.FormatArguments(arguments, 1).Trim();
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[]
			{
				sender.LogName,
				" changed server configuration: ",
				arguments.At(0),
				": ",
				text,
				"."
			}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			string text2 = arguments.At(0).ToUpper();
			if (!(text2 == "FRIENDLY_FIRE"))
			{
				if (text2 == "PLAYER_LIST_TITLE")
				{
					string text3 = text ?? string.Empty;
					PlayerList.Title.Value = text3;
					try
					{
						PlayerList.singleton.RefreshTitle();
					}
					catch (Exception ex)
					{
						if (!(ex is CommandInputException) && !(ex is InvalidOperationException))
						{
							throw;
						}
						response = "Could not set player list title [" + text3 + "]:\n" + ex.Message;
						return false;
					}
					response = string.Concat(new string[]
					{
						"Done! Config [",
						arguments.At(0),
						"] has been set to [",
						ServerConfigSynchronizer.Singleton.ServerName,
						"]!"
					});
					ServerConfigSynchronizer.RefreshAllConfigs();
					return true;
				}
				if (!(text2 == "PD_REFRESH_EXIT"))
				{
					if (!(text2 == "SPAWN_PROTECT_ENABLED"))
					{
						if (!(text2 == "SPAWN_PROTECT_TIME"))
						{
							response = "Invalid config " + arguments.At(0);
							return false;
						}
						int num;
						if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
						{
							SpawnProtected.SpawnDuration = (float)num;
							response = string.Format("Done! Config [{0}] has been set to [{1}]!", arguments.At(0), num);
							ServerConfigSynchronizer.RefreshAllConfigs();
							return true;
						}
						response = arguments.At(0) + " has invalid value, " + text + " is not a valid integer!";
						return false;
					}
					else
					{
						bool flag;
						if (bool.TryParse(text, out flag))
						{
							SpawnProtected.IsProtectionEnabled = flag;
							response = string.Format("Done! Config [{0}] has been set to [{1}]!", arguments.At(0), flag);
							return true;
						}
						if (text.Equals("toggle", StringComparison.OrdinalIgnoreCase))
						{
							SpawnProtected.IsProtectionEnabled = !SpawnProtected.IsProtectionEnabled;
							response = string.Format("Done! Config [{0}] has been set to [{1}]!", arguments.At(0), SpawnProtected.IsProtectionEnabled);
							return true;
						}
						response = arguments.At(0) + " has invalid value, " + text + " is not a valid bool!";
						return false;
					}
				}
				else
				{
					bool flag2;
					if (bool.TryParse(text, out flag2))
					{
						PocketDimensionTeleport.RefreshExit = flag2;
						response = string.Format("Done! Config [{0}] has been set to [{1}]!", arguments.At(0), flag2);
						ServerConfigSynchronizer.RefreshAllConfigs();
						return true;
					}
					response = arguments.At(0) + " has invalid value, " + text + " is not a valid bool!";
					return false;
				}
			}
			else
			{
				bool flag3;
				if (bool.TryParse(text, out flag3))
				{
					ServerConsole.FriendlyFire = flag3;
					response = string.Format("Done! Config [{0}] has been set to [{1}]!", arguments.At(0), flag3);
					ServerConfigSynchronizer.RefreshAllConfigs();
					return true;
				}
				if (text.Equals("toggle", StringComparison.OrdinalIgnoreCase))
				{
					ServerConsole.FriendlyFire = !ServerConsole.FriendlyFire;
					response = string.Format("Done! Config [{0}] has been set to [{1}]!", arguments.At(0), ServerConsole.FriendlyFire);
					ServerConfigSynchronizer.RefreshAllConfigs();
					return true;
				}
				response = arguments.At(0) + " has invalid value, " + text + " is not a valid bool!";
				return false;
			}
		}
	}
}
