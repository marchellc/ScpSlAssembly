using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils.CommandInterpolation;

public class InterpolatedCommandFormatter
{
	private class InterpolatedCommandFormatterContext
	{
		public List<string> Arguments { get; }

		public StringBuilder Builder { get; }

		public InterpolatedCommandFormatterContext()
		{
			this.Arguments = new List<string>();
			this.Builder = new StringBuilder();
		}

		public void Clear()
		{
			this.Arguments.Clear();
			this.Builder.Clear();
		}
	}

	private readonly Stack<InterpolatedCommandFormatterContext> availableContexts;

	private Dictionary<string, Func<List<string>, string>> commands;

	public char StartClosure { get; set; }

	public char EndClosure { get; set; }

	public char Escape { get; set; }

	public char ArgumentSplitter { get; set; }

	public Dictionary<string, Func<List<string>, string>> Commands
	{
		get
		{
			return this.commands;
		}
		set
		{
			this.commands = value ?? throw new ArgumentNullException("value");
		}
	}

	public InterpolatedCommandFormatter(int initialContexts = 4)
	{
		this.availableContexts = new Stack<InterpolatedCommandFormatterContext>();
		for (int i = 0; i < initialContexts; i++)
		{
			this.availableContexts.Push(new InterpolatedCommandFormatterContext());
		}
		this.Commands = new Dictionary<string, Func<List<string>, string>>();
	}

	private void ProcessInterpolation(string raw, InterpolatedCommandFormatterContext context)
	{
		bool flag = false;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < raw.Length; i++)
		{
			char c = raw[i];
			if (c == this.Escape && !flag)
			{
				flag = true;
			}
			else if (flag)
			{
				flag = false;
				switch (c)
				{
				case 'n':
					context.Builder.Append('\n');
					continue;
				case '\\':
					context.Builder.Append('\\');
					continue;
				}
				if (c != this.StartClosure && c != this.EndClosure && c != this.ArgumentSplitter && c != this.Escape)
				{
					throw new InvalidOperationException($"Unrecognized escape character at column {i}.");
				}
			}
			else if (c == this.StartClosure)
			{
				if (num++ == 0)
				{
					num2 = i + 1;
				}
			}
			else if (c == this.EndClosure)
			{
				if (num-- == 0)
				{
					throw new InvalidOperationException($"Unmatched closing character at column {i}.");
				}
				if (num != 0)
				{
					continue;
				}
				string processed = raw.Substring(num2, i - num2);
				if (!this.ProcessInterpolatedCommand(processed, context.Arguments, out processed))
				{
					throw new InvalidOperationException(string.Format("Invalid command at column {0}: {1}", num2, string.Join(", ", context.Arguments.Select((string x) => "\"" + x + "\""))));
				}
				context.Arguments.Clear();
				context.Builder.Append(processed);
			}
			else if (num == 0)
			{
				context.Builder.Append(c);
			}
		}
		if (flag)
		{
			throw new InvalidOperationException("Unable to end string with an escape character.");
		}
		if (num > 0)
		{
			throw new InvalidOperationException("Unmatched opening character(s).");
		}
	}

	private bool ProcessInterpolatedCommand(string raw, List<string> argumentBuffer, out string processed)
	{
		this.ProcessArguments(raw, argumentBuffer);
		string key = argumentBuffer[0];
		if (this.Commands.TryGetValue(key, out var value))
		{
			processed = value(argumentBuffer);
			return true;
		}
		processed = null;
		return false;
	}

	private void ProcessArguments(string raw, ICollection<string> arguments)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		for (int i = 0; i < raw.Length; i++)
		{
			char c = raw[i];
			if (flag)
			{
				flag = false;
			}
			else if (c == this.Escape)
			{
				flag = true;
			}
			else if (c == this.StartClosure)
			{
				num2++;
			}
			else if (c == this.EndClosure)
			{
				num2--;
			}
			else if (c == this.ArgumentSplitter && num2 == 0)
			{
				arguments.Add(raw.Substring(num, i - num));
				num = i + 1;
			}
		}
		arguments.Add(raw.Substring(num, raw.Length - num));
	}

	private InterpolatedCommandFormatterContext SafePopContext()
	{
		if (this.availableContexts.Count != 0)
		{
			return this.availableContexts.Pop();
		}
		return new InterpolatedCommandFormatterContext();
	}

	public bool TryProcessExpression(string raw, string source, out string result)
	{
		bool result2 = false;
		try
		{
			result = this.ProcessExpression(raw);
			result2 = true;
		}
		catch (InvalidOperationException ex)
		{
			result = "Command interpolation (" + source + ") threw an error: " + ex.Message;
		}
		catch (CommandInputException ex2)
		{
			string text = ((ex2.ArgumentValue is IEnumerable<object> source2) ? string.Join(", ", source2.Select((object x) => $"\"{x}\"")) : ex2.ArgumentValue.ToString());
			result = "A command errored in " + source + " command interpolation: " + ex2.Message + "\nArgument name: " + ex2.ArgumentName + "\nArgument value: " + text;
		}
		return result2;
	}

	public string ProcessExpression(string raw)
	{
		InterpolatedCommandFormatterContext interpolatedCommandFormatterContext = this.SafePopContext();
		this.ProcessInterpolation(raw, interpolatedCommandFormatterContext);
		string result = interpolatedCommandFormatterContext.Builder.ToString();
		interpolatedCommandFormatterContext.Clear();
		this.availableContexts.Push(interpolatedCommandFormatterContext);
		return result;
	}
}
