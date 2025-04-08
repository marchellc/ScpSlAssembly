using System;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	[CommandHandler(typeof(StripdownCommand))]
	public class StripdownComponentCommand : StripdownInstructionBase
	{
		public override string Command
		{
			get
			{
				return "component";
			}
		}

		public override string Description
		{
			get
			{
				return "Performs a GetComponent on all previously selected objects and applies them as new selections.";
			}
		}

		protected override string ProcessInstruction(NetworkConnection sender, string instruction)
		{
			string text;
			try
			{
				StripdownProcessor.SelectComponent(instruction);
				text = "Component '" + instruction + "' selected on each object.";
			}
			catch (Exception ex)
			{
				text = "Failed to get components. Exception: " + ex.Message + ";" + ex.StackTrace;
			}
			return text;
		}
	}
}
