using System;

public class ServerConsoleSender : CommandSender
{
	public override string SenderId
	{
		get
		{
			return "SERVER CONSOLE";
		}
	}

	public override string Nickname
	{
		get
		{
			return "SERVER CONSOLE";
		}
	}

	public override ulong Permissions
	{
		get
		{
			return ulong.MaxValue;
		}
	}

	public override byte KickPower
	{
		get
		{
			return byte.MaxValue;
		}
	}

	public override bool FullPermissions
	{
		get
		{
			return true;
		}
	}

	public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
	{
		this.Print(text);
	}

	public override void Print(string text)
	{
		ServerConsole.AddLog(text, ConsoleColor.Gray, false);
	}

	public override void Print(string text, ConsoleColor c)
	{
		ServerConsole.AddLog(text, c, false);
	}

	public override bool Available()
	{
		return true;
	}
}
