using System;
using GameCore;
using UnityEngine;

namespace ServerOutput
{
	public class NonDedicatedOutput : IServerOutput, IDisposable
	{
		public void Start()
		{
		}

		public void AddLog(string text, ConsoleColor color)
		{
			global::GameCore.Console.AddLog(ServerConsole.ColorText("[SRV] " + text, color), Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}

		public void AddLog(string text)
		{
			this.AddLog(text, ConsoleColor.Gray);
		}

		public void AddOutput(IOutputEntry entry)
		{
			if (entry is TextOutputEntry)
			{
				TextOutputEntry textOutputEntry = (TextOutputEntry)entry;
				this.AddLog(textOutputEntry.Text, (ConsoleColor)textOutputEntry.Color);
				return;
			}
			global::GameCore.Console.AddLog("[Control Message] " + entry.GetString(), Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}

		public void Dispose()
		{
		}
	}
}
