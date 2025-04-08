using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameCore;

namespace PlayerRoles.RoleAssign
{
	public class ScpTicketsLoader : IDisposable
	{
		private static string FilePath
		{
			get
			{
				return ConfigSharing.Paths[6] + "ScpTickets.txt";
			}
		}

		private static long UnixTimeNow
		{
			get
			{
				return DateTimeOffset.Now.ToUnixTimeSeconds();
			}
		}

		public ScpTicketsLoader()
		{
			if (ScpTicketsLoader._isBusy)
			{
				throw new InvalidOperationException("Unable to load SCP ticket entries. Another ScpTicketsLoader is active or undisposed.");
			}
			ScpTicketsLoader._isBusy = true;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				string userId = referenceHub.authManager.UserId;
				if (!string.IsNullOrEmpty(userId))
				{
					ScpTicketsLoader.PlayersByIdHash.GetOrAdd(userId.GetHashCode(), () => new List<ReferenceHub>()).Add(referenceHub);
				}
			}
			ScpTicketsLoader.LoadEntries();
		}

		public int GetTickets(ReferenceHub hub, int defaultNumber)
		{
			ScpTicketsLoader.TicketEntry ticketEntry;
			if (!ScpTicketsLoader.EntriesByUser.TryGetValue(hub, out ticketEntry))
			{
				return defaultNumber;
			}
			return ticketEntry.Tickets;
		}

		public void ModifyTickets(ReferenceHub hub, int newNumber)
		{
			ScpTicketsLoader.TicketEntry ticketEntry;
			if (ScpTicketsLoader.EntriesByUser.TryGetValue(hub, out ticketEntry))
			{
				ticketEntry.LastUpdate = ScpTicketsLoader.UnixTimeNow;
				ticketEntry.Tickets = newNumber;
				return;
			}
			ScpTicketsLoader.TicketEntry ticketEntry2 = new ScpTicketsLoader.TicketEntry
			{
				UserId = hub.authManager.UserId,
				LastUpdate = ScpTicketsLoader.UnixTimeNow,
				Tickets = newNumber
			};
			ScpTicketsLoader.AllEntries.Add(ticketEntry2);
			ScpTicketsLoader.EntriesByUser.Add(hub, ticketEntry2);
		}

		public void Dispose()
		{
			ScpTicketsLoader.SaveEntries();
			ScpTicketsLoader.AllEntries.Clear();
			ScpTicketsLoader.EntriesByUser.Clear();
			ScpTicketsLoader.PlayersByIdHash.Clear();
			ScpTicketsLoader._isBusy = false;
		}

		private static void SaveEntries()
		{
			FileManager.WriteToFile(ScpTicketsLoader.AllEntries.Select(new Func<ScpTicketsLoader.TicketEntry, string>(ScpTicketsLoader.SerializeEntry)), ScpTicketsLoader.FilePath, false);
		}

		private static void LoadEntries()
		{
			if (!File.Exists(ScpTicketsLoader.FilePath))
			{
				return;
			}
			long unixTimeNow = ScpTicketsLoader.UnixTimeNow;
			using (FileStream fileStream = new FileStream(ScpTicketsLoader.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (StreamReader streamReader = new StreamReader(fileStream))
				{
					for (;;)
					{
						string text = streamReader.ReadLine();
						if (text == null)
						{
							break;
						}
						ScpTicketsLoader.TicketEntry ticketEntry;
						if (ScpTicketsLoader.TryDeserialize(text, out ticketEntry) && ticketEntry.LastUpdate + 1296000L >= unixTimeNow)
						{
							ScpTicketsLoader.AddEntry(ticketEntry);
						}
					}
				}
			}
		}

		private static void AddEntry(ScpTicketsLoader.TicketEntry entry)
		{
			ScpTicketsLoader.AllEntries.Add(entry);
			int hashCode = entry.UserId.GetHashCode();
			List<ReferenceHub> list;
			if (!ScpTicketsLoader.PlayersByIdHash.TryGetValue(hashCode, out list))
			{
				return;
			}
			foreach (ReferenceHub referenceHub in list)
			{
				if (!(referenceHub.authManager.UserId != entry.UserId))
				{
					ScpTicketsLoader.EntriesByUser[referenceHub] = entry;
				}
			}
		}

		private static string SerializeEntry(ScpTicketsLoader.TicketEntry entry)
		{
			return string.Concat(new object[] { entry.UserId, ';', entry.Tickets, ';', entry.LastUpdate });
		}

		private static bool TryDeserialize(string text, out ScpTicketsLoader.TicketEntry entry)
		{
			entry = null;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string[] array = text.Split(';', StringSplitOptions.None);
			if (array.Length != 3)
			{
				return false;
			}
			int num;
			if (!int.TryParse(array[1], out num))
			{
				return false;
			}
			long num2;
			if (!long.TryParse(array[2], out num2))
			{
				return false;
			}
			entry = new ScpTicketsLoader.TicketEntry
			{
				UserId = array[0],
				Tickets = num,
				LastUpdate = num2
			};
			return true;
		}

		private static readonly List<ScpTicketsLoader.TicketEntry> AllEntries = new List<ScpTicketsLoader.TicketEntry>(65535);

		private static readonly Dictionary<ReferenceHub, ScpTicketsLoader.TicketEntry> EntriesByUser = new Dictionary<ReferenceHub, ScpTicketsLoader.TicketEntry>();

		private static readonly Dictionary<int, List<ReferenceHub>> PlayersByIdHash = new Dictionary<int, List<ReferenceHub>>();

		private static bool _isBusy;

		private const string Filename = "ScpTickets.txt";

		private const int ExpectedCapacity = 65535;

		private const int ExpirationTime = 1296000;

		private const char Separator = ';';

		private class TicketEntry
		{
			public string UserId;

			public int Tickets;

			public long LastUpdate;
		}
	}
}
