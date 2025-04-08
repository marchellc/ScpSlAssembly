using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class CassieWordsCommand : ICommand
	{
		public string Command { get; } = "cassiewords";

		public string[] Aliases { get; }

		public string Description { get; } = "Lists CASSIE words.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			bool flag = false;
			Regex regex = null;
			if (arguments.Count > 0)
			{
				regex = new Regex(arguments.FirstElement<string>(), RegexOptions.IgnoreCase);
				flag = true;
			}
			string text = "CASSIE words: ";
			string text2;
			if (!flag)
			{
				text2 = string.Join(" ", NineTailedFoxAnnouncer.singleton.voiceLines.Select((NineTailedFoxAnnouncer.VoiceLine line) => line.apiName));
			}
			else
			{
				text2 = string.Join(" ", from line in NineTailedFoxAnnouncer.singleton.voiceLines
					select line.apiName into line
					where regex.IsMatch(line)
					select line);
			}
			response = text + text2;
			return true;
		}
	}
}
