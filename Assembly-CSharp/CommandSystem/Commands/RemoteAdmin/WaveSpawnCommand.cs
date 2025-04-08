using System;
using System.Text;
using NorthwoodLib.Pools;
using Respawning;
using Respawning.Waves;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(WaveCommand))]
	public class WaveSpawnCommand : TargetWaveCommandBase
	{
		public override string Command { get; } = "spawn";

		public override string[] Aliases { get; } = new string[] { "sp", "fs", "force" };

		public override string Description { get; } = "Sets the value of the specified flag.";

		public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.RespawnEvents, out response))
			{
				return false;
			}
			bool flag = false;
			if (arguments.Count <= 0)
			{
				response = this.InvalidFlagMessage("Not enough arguments");
				return false;
			}
			if (arguments.Count > 1)
			{
				bool flag2;
				flag = bool.TryParse(arguments.At(1), out flag2) && flag2;
			}
			string text = TargetWaveCommandBase.TranslateWaveName(arguments.At(0));
			bool flag3 = false;
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				string name = spawnableWaveBase.GetType().Name;
				if (name.Equals(text, StringComparison.OrdinalIgnoreCase))
				{
					if (flag)
					{
						WaveManager.Spawn(spawnableWaveBase);
					}
					else
					{
						WaveManager.InitiateRespawn(spawnableWaveBase);
					}
					text = name;
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				response = this.InvalidFlagMessage("No wave was found");
				return false;
			}
			string text2 = (flag ? "force spawned" : "spawned");
			response = string.Concat(new string[] { "Action completed, ", text, " was ", text2, "." });
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Concat(new string[] { sender.LogName, " ", text2, " ", text, "." }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			return true;
		}

		private string InvalidFlagMessage(string context)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.Append(context);
			stringBuilder.Append(". The correct syntax is \"wave ");
			stringBuilder.Append(this.Command);
			stringBuilder.Append(" <wave> [forceSpawn? (true/false)]");
			return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		}
	}
}
