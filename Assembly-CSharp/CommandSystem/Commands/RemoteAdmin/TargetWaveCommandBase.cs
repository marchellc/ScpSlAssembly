using System;
using System.Collections.Generic;
using Respawning.Waves;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(WaveCommand))]
	public abstract class TargetWaveCommandBase : ICommand
	{
		public abstract string Command { get; }

		public abstract string[] Aliases { get; }

		public abstract string Description { get; }

		public abstract bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response);

		protected static string TranslateWaveName(string input)
		{
			foreach (KeyValuePair<Type, string[]> keyValuePair in TargetWaveCommandBase.WaveAliases)
			{
				if (keyValuePair.Value.Contains(input, StringComparison.OrdinalIgnoreCase))
				{
					return keyValuePair.Key.Name;
				}
			}
			return input;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static TargetWaveCommandBase()
		{
			Dictionary<Type, string[]> dictionary = new Dictionary<Type, string[]>();
			Type typeFromHandle = typeof(NtfSpawnWave);
			dictionary[typeFromHandle] = new string[] { "NTF", "MTF", "MobileTaskForces", "NineTailedFox" };
			Type typeFromHandle2 = typeof(ChaosSpawnWave);
			dictionary[typeFromHandle2] = new string[] { "CI", "Chaos", "ChaosInsurgency" };
			Type typeFromHandle3 = typeof(NtfMiniWave);
			dictionary[typeFromHandle3] = new string[] { "NTFMini", "MTFMini", "MiniMTF", "MiniNTF" };
			Type typeFromHandle4 = typeof(ChaosMiniWave);
			dictionary[typeFromHandle4] = new string[] { "ChaosMini", "CIMini", "MiniCI", "MiniChaos" };
			TargetWaveCommandBase.WaveAliases = dictionary;
		}

		private static readonly Dictionary<Type, string[]> WaveAliases;
	}
}
