using System;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	[CommandHandler(typeof(StripdownCommand))]
	public class StripdownValueCommand : StripdownInstructionBase
	{
		public override string Command
		{
			get
			{
				return "value";
			}
		}

		public override string Description
		{
			get
			{
				return "Selects (by name) properties or fields of previously selected objects.";
			}
		}

		protected override string ProcessInstruction(NetworkConnection sender, string instruction)
		{
			string text;
			try
			{
				StripdownProcessor.SelectValues(instruction);
				text = "Value '" + instruction + "' selected on each object.";
			}
			catch (Exception ex)
			{
				text = "Failed to select value. Exception: " + ex.Message + ";" + ex.StackTrace;
			}
			return text;
		}
	}
}
