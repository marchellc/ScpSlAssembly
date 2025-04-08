using System;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	[CommandHandler(typeof(StripdownCommand))]
	public class StripdownObjectCommand : StripdownInstructionBase
	{
		public override string Command
		{
			get
			{
				return "object";
			}
		}

		public override string Description
		{
			get
			{
				return "Finds all objects of provided type and selects them.";
			}
		}

		protected override string ProcessInstruction(NetworkConnection sender, string instruction)
		{
			string text;
			try
			{
				int num = StripdownProcessor.SelectUnityObjects(instruction);
				text = string.Format("Selected {0} objects of type {1}", num, instruction);
			}
			catch (Exception ex)
			{
				text = "Failed to select objects. Exception: " + ex.Message + ";" + ex.StackTrace;
			}
			return text;
		}
	}
}
