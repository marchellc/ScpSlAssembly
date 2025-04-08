using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(WaveCommand))]
	public class WaveSetCommand : TargetWaveCommandBase
	{
		public override string Command { get; } = "set";

		public override string[] Aliases { get; } = new string[] { "st", "s" };

		public override string Description { get; } = "Sets the value of the specified flag.";

		public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.RespawnEvents, out response))
			{
				return false;
			}
			if (arguments.Count <= 2)
			{
				response = this.InvalidFlagMessage("Not enough arguments");
				return false;
			}
			string text = TargetWaveCommandBase.TranslateWaveName(arguments.At(0));
			UpdateMessageFlags updateMessageFlags;
			if (!Enum.TryParse<UpdateMessageFlags>(arguments.At(1), true, out updateMessageFlags))
			{
				response = this.InvalidFlagMessage("Specified flag does not exist");
				return false;
			}
			Func<SpawnableWaveBase, string, bool> func;
			if (!WaveSetCommand.MethodPerFlag.TryGetValue(updateMessageFlags, out func))
			{
				response = this.InvalidFlagMessage("Specified flag has no specified action");
				return false;
			}
			string text2 = arguments.At(2);
			bool flag = false;
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				string name = spawnableWaveBase.GetType().Name;
				if (name.Equals(text, StringComparison.OrdinalIgnoreCase) && func(spawnableWaveBase, text2))
				{
					text = name;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				response = this.InvalidFlagMessage("No wave was found");
				return false;
			}
			response = string.Format("Set {0}'s {1} to {2}.", text, updateMessageFlags, text2);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} set {1}'s {2} to {3}.", new object[] { sender.LogName, text, updateMessageFlags, text2 }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			return true;
		}

		private string InvalidFlagMessage(string context)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.Append(context);
			stringBuilder.Append(". The correct syntax is \"wave ");
			stringBuilder.Append(this.Command);
			stringBuilder.Append(" <wave> <flag> <value>\". Available flags:");
			foreach (UpdateMessageFlags updateMessageFlags in WaveSetCommand.MethodPerFlag.Keys)
			{
				stringBuilder.Append("\n- ");
				stringBuilder.Append(updateMessageFlags);
			}
			return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		}

		private static bool SetTokens(SpawnableWaveBase wave, string value)
		{
			ILimitedWave limitedWave = wave as ILimitedWave;
			if (limitedWave == null)
			{
				return false;
			}
			int num;
			if (!int.TryParse(value, out num))
			{
				return false;
			}
			limitedWave.RespawnTokens = num;
			return true;
		}

		private static bool SetTimer(SpawnableWaveBase wave, string value)
		{
			TimeBasedWave timeBasedWave = wave as TimeBasedWave;
			if (timeBasedWave == null)
			{
				return false;
			}
			float num;
			if (!float.TryParse(value, out num))
			{
				return false;
			}
			timeBasedWave.Timer.SetTime(num);
			return true;
		}

		private static bool SetPause(SpawnableWaveBase wave, string value)
		{
			TimeBasedWave timeBasedWave = wave as TimeBasedWave;
			if (timeBasedWave == null)
			{
				return false;
			}
			float num;
			if (!float.TryParse(value, out num))
			{
				return false;
			}
			timeBasedWave.Timer.Pause(num);
			return true;
		}

		private static bool PlayAnimation(SpawnableWaveBase wave, string _)
		{
			if (!(wave is IAnimatedWave))
			{
				return false;
			}
			WaveUpdateMessage.ServerSendUpdate(wave, UpdateMessageFlags.Trigger);
			return true;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static WaveSetCommand()
		{
			Dictionary<UpdateMessageFlags, Func<SpawnableWaveBase, string, bool>> dictionary = new Dictionary<UpdateMessageFlags, Func<SpawnableWaveBase, string, bool>>();
			dictionary[UpdateMessageFlags.Pause] = (SpawnableWaveBase w, string input) => WaveSetCommand.SetPause(w, input);
			dictionary[UpdateMessageFlags.Timer] = (SpawnableWaveBase w, string input) => WaveSetCommand.SetTimer(w, input);
			dictionary[UpdateMessageFlags.Tokens] = (SpawnableWaveBase w, string input) => WaveSetCommand.SetTokens(w, input);
			dictionary[UpdateMessageFlags.Trigger] = (SpawnableWaveBase w, string input) => WaveSetCommand.PlayAnimation(w, input);
			WaveSetCommand.MethodPerFlag = dictionary;
		}

		public static Dictionary<UpdateMessageFlags, Func<SpawnableWaveBase, string, bool>> MethodPerFlag;
	}
}
