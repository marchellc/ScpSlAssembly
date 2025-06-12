using System;

namespace Utils.CommandInterpolation;

public class CommandInputException : Exception
{
	public string ArgumentName { get; }

	public object ArgumentValue { get; }

	public CommandInputException(string argName, object argValue, string message)
		: base(message)
	{
		this.ArgumentName = argName;
		this.ArgumentValue = argValue;
	}
}
