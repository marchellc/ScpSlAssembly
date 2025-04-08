using System;
using CentralAuth;
using Mirror;
using Utils.NonAllocLINQ;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class IdleCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "idle";

		public string[] Aliases { get; } = new string[] { "i" };

		public string Description { get; } = "Controls server idle mode";

		public string[] Usage { get; } = new string[] { "Enable/Disable/ForceEnable (Optional)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!ServerStatic.IsDedicated)
			{
				response = "This command can only be executed on a dedicated server.";
				return false;
			}
			if (!NetworkServer.active)
			{
				response = "This command can only be used on a server.";
				return false;
			}
			if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			if (arguments.Count == 0)
			{
				response = "Server is " + (IdleMode.IdleModeActive ? string.Empty : "**NOT** ") + "currently in idle mode.";
				return true;
			}
			response = "";
			string text = arguments.At(0).ToUpperInvariant();
			uint num = <PrivateImplementationDetails>.ComputeStringHash(text);
			if (num <= 2311963226U)
			{
				if (num <= 2278407988U)
				{
					if (num != 785938062U)
					{
						if (num != 2278407988U)
						{
							goto IL_02AC;
						}
						if (!(text == "-D"))
						{
							goto IL_02AC;
						}
						goto IL_026D;
					}
					else if (!(text == "ENABLE"))
					{
						goto IL_02AC;
					}
				}
				else if (num != 2295185607U)
				{
					if (num != 2311963226U)
					{
						goto IL_02AC;
					}
					if (!(text == "-F"))
					{
						goto IL_02AC;
					}
					goto IL_022E;
				}
				else if (!(text == "-E"))
				{
					goto IL_02AC;
				}
			}
			else if (num <= 3222007936U)
			{
				if (num != 2616878531U)
				{
					if (num != 3222007936U)
					{
						goto IL_02AC;
					}
					if (!(text == "E"))
					{
						goto IL_02AC;
					}
				}
				else
				{
					if (!(text == "DISABLE"))
					{
						goto IL_02AC;
					}
					goto IL_026D;
				}
			}
			else if (num != 3238785555U)
			{
				if (num != 3272340793U)
				{
					if (num != 3792880932U)
					{
						goto IL_02AC;
					}
					if (!(text == "FORCE"))
					{
						goto IL_02AC;
					}
					goto IL_022E;
				}
				else
				{
					if (!(text == "F"))
					{
						goto IL_02AC;
					}
					goto IL_022E;
				}
			}
			else
			{
				if (!(text == "D"))
				{
					goto IL_02AC;
				}
				goto IL_026D;
			}
			if (IdleMode.IdleModeActive)
			{
				response = "Server is already in the idle mode.";
				return false;
			}
			if (ReferenceHub.AllHubs.Any((ReferenceHub x) => x.Mode == ClientInstanceMode.ReadyClient))
			{
				response = "You can't enable the idle mode when players are connected to the server.";
				return false;
			}
			if (!(sender is ServerConsoleSender))
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " enabled the idle mode.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				response = "Idle mode enabled.";
			}
			IdleMode.SetIdleMode(true);
			return true;
			IL_022E:
			if (IdleMode.IdleModeActive)
			{
				response = "Server is already in the idle mode.";
				return false;
			}
			if (!(sender is ServerConsoleSender))
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " force enabled the idle mode.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				response = "Idle mode force enabled.";
			}
			IdleMode.SetIdleMode(true);
			return true;
			IL_026D:
			if (!IdleMode.IdleModeActive)
			{
				response = "Server isn't in idle mode.";
				return false;
			}
			if (!(sender is ServerConsoleSender))
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " disabled the idle mode.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				response = "Idle mode disabled";
			}
			IdleMode.SetIdleMode(false);
			return true;
			IL_02AC:
			response = string.Concat(new string[]
			{
				"Unknown subcommand.\nUsage: ",
				this.Command,
				" ",
				this.DisplayCommandUsage(),
				"."
			});
			return false;
		}
	}
}
