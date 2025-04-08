using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using GameCore;
using UnityEngine;

namespace ServerOutput
{
	public class FileConsole : IServerOutput, IDisposable
	{
		public FileConsole(string session)
		{
			this._session = (string.IsNullOrEmpty(session) ? "default" : session);
			this._queueThread = new Thread(new ThreadStart(this.Prompt))
			{
				Priority = global::System.Threading.ThreadPriority.Lowest,
				IsBackground = true,
				Name = "Dedicated server console output"
			};
			this._fsw = new FileSystemWatcher
			{
				Path = "SCPSL_Data/Dedicated/" + this._session,
				NotifyFilter = NotifyFilters.FileName
			};
		}

		public void Start()
		{
			if (Directory.Exists("SCPSL_Data/Dedicated/" + this._session) && Environment.GetCommandLineArgs().Contains("-nodedicateddelete"))
			{
				string[] files = Directory.GetFiles("SCPSL_Data/Dedicated/" + this._session);
				for (int i = 0; i < files.Length; i++)
				{
					File.Delete(files[i]);
				}
			}
			Directory.CreateDirectory("SCPSL_Data/Dedicated/" + this._session);
			this._queueThread.Start();
			this._fsw.Created += delegate(object sender, FileSystemEventArgs args)
			{
				if (args.Name.Contains("cs") && args.Name.Contains("mapi"))
				{
					new Thread(delegate
					{
						this.ReadLog(args.FullPath);
					}).Start();
				}
			};
			this._fsw.EnableRaisingEvents = true;
		}

		public void Dispose()
		{
			this._disposing = true;
			this._fsw.Dispose();
			try
			{
				if (this._queueThread.IsAlive)
				{
					this._queueThread.Abort();
				}
			}
			catch
			{
			}
			if (!ServerStatic.KeepSession && Directory.Exists("SCPSL_Data/Dedicated/" + this._session))
			{
				Directory.Delete("SCPSL_Data/Dedicated/" + this._session, true);
			}
		}

		private void ReadLog(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					string text = path.Remove(0, path.IndexOf("cs", StringComparison.Ordinal));
					string empty = string.Empty;
					string text2 = string.Empty;
					ConsoleColor consoleColor = ConsoleColor.Gray;
					try
					{
						text2 = "Error while reading the file: " + text;
						using (StreamReader streamReader = new StreamReader("SCPSL_Data/Dedicated/" + this._session + "/" + text))
						{
							string text3 = streamReader.ReadToEnd();
							text2 = "Error while dedecting 'terminator end-of-message' signal.";
							if (text3.Contains("terminator"))
							{
								text3 = text3.Remove(text3.LastIndexOf("terminator", StringComparison.Ordinal));
							}
							text2 = "Error while sending message.";
							ServerConsole.PrompterQueue.Enqueue(text3);
							File.Delete("SCPSL_Data/Dedicated/" + this._session + "/" + text);
							return;
						}
					}
					catch
					{
						Debug.LogError("Error in server console: " + text2);
					}
					if (!string.IsNullOrEmpty(empty))
					{
						this.AddLog(empty, consoleColor);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		public void AddLog(string text, ConsoleColor color)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (ServerStatic.IsDedicated)
			{
				this._prompterQueue.Enqueue(new TextOutputEntry(text, color));
				return;
			}
			global::GameCore.Console.AddLog(text, Color.grey, false, global::GameCore.Console.ConsoleLogType.Log);
		}

		public void AddLog(string text)
		{
			this.AddLog(text, ConsoleColor.Gray);
		}

		public void AddOutput(IOutputEntry entry)
		{
			if (ServerStatic.IsDedicated)
			{
				this._prompterQueue.Enqueue(entry);
			}
		}

		private void Prompt()
		{
			while (!this._disposing)
			{
				IOutputEntry outputEntry;
				if (this._prompterQueue.Count == 0)
				{
					Thread.Sleep(25);
				}
				else if (this._prompterQueue.TryDequeue(out outputEntry))
				{
					StreamWriter streamWriter = new StreamWriter(string.Concat(new string[]
					{
						"SCPSL_Data/Dedicated/",
						this._session,
						"/sl",
						this._logId.ToString(),
						".mapi"
					}));
					this._logId += 1U;
					streamWriter.WriteLine(outputEntry.ToString());
					streamWriter.Close();
				}
			}
		}

		private bool _disposing;

		private uint _logId;

		private readonly string _session;

		private readonly FileSystemWatcher _fsw;

		private readonly Thread _queueThread;

		private readonly ConcurrentQueue<IOutputEntry> _prompterQueue = new ConcurrentQueue<IOutputEntry>();
	}
}
