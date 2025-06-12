using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;

namespace CommandSystem.Commands.RemoteAdmin;

[CommandHandler(typeof(WaveCommand))]
public class WaveSetCommand : TargetWaveCommandBase
{
	public static Dictionary<UpdateMessageFlags, Func<SpawnableWaveBase, string, bool>> MethodPerFlag = new Dictionary<UpdateMessageFlags, Func<SpawnableWaveBase, string, bool>>
	{
		[UpdateMessageFlags.Pause] = (SpawnableWaveBase w, string input) => WaveSetCommand.SetPause(w, input),
		[UpdateMessageFlags.Timer] = (SpawnableWaveBase w, string input) => WaveSetCommand.SetTimer(w, input),
		[UpdateMessageFlags.Tokens] = (SpawnableWaveBase w, string input) => WaveSetCommand.SetTokens(w, input),
		[UpdateMessageFlags.Trigger] = (SpawnableWaveBase w, string input) => WaveSetCommand.PlayAnimation(w, input)
	};

	public override string Command { get; } = "set";

	public override string[] Aliases { get; } = new string[2] { "st", "s" };

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
		if (!Enum.TryParse<UpdateMessageFlags>(arguments.At(1), ignoreCase: true, out var result))
		{
			response = this.InvalidFlagMessage("Specified flag does not exist");
			return false;
		}
		if (!WaveSetCommand.MethodPerFlag.TryGetValue(result, out var value))
		{
			response = this.InvalidFlagMessage("Specified flag has no specified action");
			return false;
		}
		string text2 = arguments.At(2);
		bool flag = false;
		foreach (SpawnableWaveBase wave in WaveManager.Waves)
		{
			string name = wave.GetType().Name;
			if (name.Equals(text, StringComparison.OrdinalIgnoreCase) && value(wave, text2))
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
		response = $"Set {text}'s {result} to {text2}.";
		ServerLogs.AddLog(ServerLogs.Modules.Administrative, $"{sender.LogName} set {text}'s {result} to {text2}.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging);
		return true;
	}

	private string InvalidFlagMessage(string context)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		stringBuilder.Append(context);
		stringBuilder.Append(". The correct syntax is \"wave ");
		stringBuilder.Append(this.Command);
		stringBuilder.Append(" <wave> <flag> <value>\". Available flags:");
		foreach (UpdateMessageFlags key in WaveSetCommand.MethodPerFlag.Keys)
		{
			stringBuilder.Append("\n- ");
			stringBuilder.Append(key);
		}
		return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	private static bool SetTokens(SpawnableWaveBase wave, string value)
	{
		if (!(wave is ILimitedWave limitedWave))
		{
			return false;
		}
		if (!int.TryParse(value, out var result))
		{
			return false;
		}
		limitedWave.RespawnTokens = result;
		return true;
	}

	private static bool SetTimer(SpawnableWaveBase wave, string value)
	{
		if (!(wave is TimeBasedWave timeBasedWave))
		{
			return false;
		}
		if (!float.TryParse(value, out var result))
		{
			return false;
		}
		timeBasedWave.Timer.SetTime(result);
		return true;
	}

	private static bool SetPause(SpawnableWaveBase wave, string value)
	{
		if (!(wave is TimeBasedWave timeBasedWave))
		{
			return false;
		}
		if (!float.TryParse(value, out var result))
		{
			return false;
		}
		timeBasedWave.Timer.Pause(result);
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
}
