using System;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	[CommandHandler(typeof(StripdownCommand))]
	public class StripdownPrintCommand : StripdownInstructionBase
	{
		public override string Command
		{
			get
			{
				return "print";
			}
		}

		public override string Description
		{
			get
			{
				return "Calls the ToString method on fields or properties of provided names. You can print out multiple values of the same object.";
			}
		}

		protected override string ProcessInstruction(NetworkConnection sender, string instruction)
		{
			string text;
			try
			{
				string[] array = StripdownProcessor.Print(instruction.Split(' ', StringSplitOptions.None));
				StripdownNetworking.StripdownResponse stripdownResponse = new StripdownNetworking.StripdownResponse
				{
					Lines = array
				};
				if (sender == null)
				{
					StripdownNetworking.ProcessMessage(stripdownResponse);
				}
				else
				{
					sender.Send<StripdownNetworking.StripdownResponse>(stripdownResponse, 0);
				}
				text = "Values printed into the debug console (or saved into a game data file).";
			}
			catch (Exception ex)
			{
				text = "Failed to print value. Exception: " + ex.Message + ";" + ex.StackTrace;
			}
			return text;
		}
	}
}
