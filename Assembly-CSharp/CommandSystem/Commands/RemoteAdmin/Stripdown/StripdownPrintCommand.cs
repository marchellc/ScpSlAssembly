using System;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown;

[CommandHandler(typeof(StripdownCommand))]
public class StripdownPrintCommand : StripdownInstructionBase
{
	public override string Command => "print";

	public override string Description => "Calls the ToString method on fields or properties of provided names. You can print out multiple values of the same object.";

	protected override string ProcessInstruction(NetworkConnection sender, string instruction)
	{
		try
		{
			string[] lines = StripdownProcessor.Print(instruction.Split(' '));
			StripdownNetworking.StripdownResponse stripdownResponse = default(StripdownNetworking.StripdownResponse);
			stripdownResponse.Lines = lines;
			StripdownNetworking.StripdownResponse stripdownResponse2 = stripdownResponse;
			if (sender == null)
			{
				StripdownNetworking.ProcessMessage(stripdownResponse2);
			}
			else
			{
				sender.Send(stripdownResponse2);
			}
			return "Values printed into the debug console (or saved into a game data file).";
		}
		catch (Exception ex)
		{
			return "Failed to print value. Exception: " + ex.Message + ";" + ex.StackTrace;
		}
	}
}
