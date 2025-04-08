using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils.CommandInterpolation
{
	public class InterpolatedCommandFormatter
	{
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
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.commands = value;
			}
		}

		public InterpolatedCommandFormatter(int initialContexts = 4)
		{
			this.availableContexts = new Stack<InterpolatedCommandFormatter.InterpolatedCommandFormatterContext>();
			for (int i = 0; i < initialContexts; i++)
			{
				this.availableContexts.Push(new InterpolatedCommandFormatter.InterpolatedCommandFormatterContext());
			}
			this.Commands = new Dictionary<string, Func<List<string>, string>>();
		}

		private void ProcessInterpolation(string raw, InterpolatedCommandFormatter.InterpolatedCommandFormatterContext context)
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
					if (c == 'n')
					{
						context.Builder.Append('\n');
					}
					else if (c == '\\')
					{
						context.Builder.Append('\\');
					}
					else if (c != this.StartClosure && c != this.EndClosure && c != this.ArgumentSplitter && c != this.Escape)
					{
						throw new InvalidOperationException(string.Format("Unrecognized escape character at column {0}.", i));
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
						throw new InvalidOperationException(string.Format("Unmatched closing character at column {0}.", i));
					}
					if (num == 0)
					{
						string text = raw.Substring(num2, i - num2);
						if (!this.ProcessInterpolatedCommand(text, context.Arguments, out text))
						{
							throw new InvalidOperationException(string.Format("Invalid command at column {0}: {1}", num2, string.Join(", ", context.Arguments.Select((string x) => "\"" + x + "\""))));
						}
						context.Arguments.Clear();
						context.Builder.Append(text);
					}
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
			string text = argumentBuffer[0];
			Func<List<string>, string> func;
			if (this.Commands.TryGetValue(text, out func))
			{
				processed = func(argumentBuffer);
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

		private InterpolatedCommandFormatter.InterpolatedCommandFormatterContext SafePopContext()
		{
			if (this.availableContexts.Count != 0)
			{
				return this.availableContexts.Pop();
			}
			return new InterpolatedCommandFormatter.InterpolatedCommandFormatterContext();
		}

		public bool TryProcessExpression(string raw, string source, out string result)
		{
			bool flag = false;
			try
			{
				result = this.ProcessExpression(raw);
				flag = true;
			}
			catch (InvalidOperationException ex)
			{
				result = "Command interpolation (" + source + ") threw an error: " + ex.Message;
			}
			catch (CommandInputException ex2)
			{
				IEnumerable<object> enumerable = ex2.ArgumentValue as IEnumerable<object>;
				string text;
				if (enumerable == null)
				{
					text = ex2.ArgumentValue.ToString();
				}
				else
				{
					text = string.Join(", ", enumerable.Select((object x) => string.Format("\"{0}\"", x)));
				}
				string text2 = text;
				result = string.Concat(new string[] { "A command errored in ", source, " command interpolation: ", ex2.Message, "\nArgument name: ", ex2.ArgumentName, "\nArgument value: ", text2 });
			}
			return flag;
		}

		public string ProcessExpression(string raw)
		{
			InterpolatedCommandFormatter.InterpolatedCommandFormatterContext interpolatedCommandFormatterContext = this.SafePopContext();
			this.ProcessInterpolation(raw, interpolatedCommandFormatterContext);
			string text = interpolatedCommandFormatterContext.Builder.ToString();
			interpolatedCommandFormatterContext.Clear();
			this.availableContexts.Push(interpolatedCommandFormatterContext);
			return text;
		}

		private readonly Stack<InterpolatedCommandFormatter.InterpolatedCommandFormatterContext> availableContexts;

		private Dictionary<string, Func<List<string>, string>> commands;

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
	}
}
