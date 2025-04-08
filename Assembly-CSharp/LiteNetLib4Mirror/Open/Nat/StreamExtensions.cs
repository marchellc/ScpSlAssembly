using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal static class StreamExtensions
	{
		internal static string ReadAsMany(this StreamReader stream, int bytesToRead)
		{
			char[] array = new char[bytesToRead];
			stream.ReadBlock(array, 0, bytesToRead);
			return new string(array);
		}

		internal static string GetXmlElementText(this XmlNode node, string elementName)
		{
			XmlElement xmlElement = node[elementName];
			if (xmlElement == null)
			{
				return string.Empty;
			}
			return xmlElement.InnerText;
		}

		internal static bool ContainsIgnoreCase(this string s, string pattern)
		{
			return s.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		internal static void LogInfo(this TraceSource source, string format, params object[] args)
		{
			try
			{
			}
			catch (ObjectDisposedException)
			{
				source.Switch.Level = SourceLevels.Off;
			}
		}

		internal static void LogWarn(this TraceSource source, string format, params object[] args)
		{
			try
			{
			}
			catch (ObjectDisposedException)
			{
				source.Switch.Level = SourceLevels.Off;
			}
		}

		internal static void LogError(this TraceSource source, string format, params object[] args)
		{
			try
			{
			}
			catch (ObjectDisposedException)
			{
				source.Switch.Level = SourceLevels.Off;
			}
		}

		internal static string ToPrintableXml(this XmlDocument document)
		{
			string text;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.Unicode))
				{
					try
					{
						xmlTextWriter.Formatting = Formatting.Indented;
						document.WriteContentTo(xmlTextWriter);
						xmlTextWriter.Flush();
						memoryStream.Flush();
						memoryStream.Position = 0L;
						text = new StreamReader(memoryStream).ReadToEnd();
					}
					catch (Exception)
					{
						text = document.ToString();
					}
				}
			}
			return text;
		}

		public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
		{
			CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource();
			TaskAwaiter<Task> taskAwaiter = Task.WhenAny(new Task[]
			{
				task,
				Task.Delay(timeout, timeoutCancellationTokenSource.Token)
			}).GetAwaiter();
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<Task> taskAwaiter2;
				taskAwaiter = taskAwaiter2;
				taskAwaiter2 = default(TaskAwaiter<Task>);
			}
			if (taskAwaiter.GetResult() == task)
			{
				timeoutCancellationTokenSource.Cancel();
				return await task;
			}
			throw new TimeoutException("The operation has timed out. The network is broken, router has gone or is too busy.");
		}
	}
}
