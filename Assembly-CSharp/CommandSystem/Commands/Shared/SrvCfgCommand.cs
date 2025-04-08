using System;
using System.Text;
using CentralAuth;
using GameCore;
using Mirror.LiteNetLib4Mirror;
using NorthwoodLib.Pools;
using RemoteAdmin;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(ClientCommandHandler))]
	public class SrvCfgCommand : ICommand
	{
		public string Command { get; } = "srvcfg";

		public string[] Aliases { get; }

		public string Description { get; } = "Displays the server config.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			bool flag;
			try
			{
				YamlConfig serverConfig = ConfigFile.ServerConfig;
				PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
				if (playerCommandSender == null || playerCommandSender.ReferenceHub.authManager.BypassBansFlagSet || playerCommandSender.ReferenceHub.isLocalPlayer || PermissionsHandler.IsPermitted(playerCommandSender.ReferenceHub.serverRoles.Permissions, PlayerPermissions.ServerConsoleCommands | PlayerPermissions.ServerConfigs))
				{
					stringBuilder.AppendLine("Extended server configuration:");
					stringBuilder.AppendFormat("Server name: {0}\n", serverConfig.GetString("server_name", ""));
					stringBuilder.AppendFormat("Server IP: {0}\n", serverConfig.GetString("server_ip", ""));
					stringBuilder.AppendFormat("Current Server IP: {0}\n", ServerConsole.Ip);
					stringBuilder.AppendFormat("Server port: {0}\n", LiteNetLib4MirrorTransport.Singleton.port);
					stringBuilder.AppendFormat("Server pastebin ID: {0}\n", serverConfig.GetString("serverinfo_pastebin_id", ""));
					stringBuilder.AppendFormat("Server max players: {0}\n", serverConfig.GetInt("max_players", 0));
					stringBuilder.AppendFormat("Online mode: {0}\n", PlayerAuthenticationManager.OnlineMode);
					stringBuilder.AppendFormat("RA password authentication: {0}\n", ReferenceHub.HostHub.queryProcessor.OverridePasswordEnabled);
					stringBuilder.AppendFormat("IP banning: {0}\n", serverConfig.GetBool("ip_banning", false));
					stringBuilder.AppendFormat("Whitelist: {0}\n", serverConfig.GetBool("enable_whitelist", false));
					stringBuilder.AppendFormat("Query status: {0} with port shift {1}\n", serverConfig.GetBool("enable_query", false), serverConfig.GetInt("query_port_shift", 0));
					stringBuilder.AppendFormat("Friendly fire: {0}\n", ServerConsole.FriendlyFire);
					stringBuilder.AppendFormat("Map seed: {0}\n", serverConfig.GetInt("map_seed", 0));
				}
				else
				{
					stringBuilder.AppendLine("Basic server configuration:");
					stringBuilder.AppendFormat("Server name: {0}\n", serverConfig.GetString("server_name", ""));
					stringBuilder.AppendFormat("Server pastebin ID: {0}\n", serverConfig.GetString("serverinfo_pastebin_id", ""));
					stringBuilder.AppendFormat("Server max players: {0}\n", serverConfig.GetInt("max_players", 0));
					stringBuilder.AppendFormat("RA password authentication: {0}\n", ReferenceHub.HostHub.queryProcessor.OverridePasswordEnabled);
					stringBuilder.AppendFormat("Online mode: {0}\n", PlayerAuthenticationManager.OnlineMode);
					stringBuilder.AppendFormat("Whitelist: {0}\n", serverConfig.GetBool("enable_whitelist", false));
					stringBuilder.AppendFormat("Friendly fire: {0}\n", ServerConsole.FriendlyFire);
				}
				flag = true;
			}
			finally
			{
				response = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
			}
			return flag;
		}
	}
}
