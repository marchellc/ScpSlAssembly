using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameCore;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using NorthwoodLib.Pools;

public static class BanHandler
{
	public static BanHandler.BanType GetBanType(int type)
	{
		if (type > EnumUtils<BanHandler.BanType>.Values.ToArray<BanHandler.BanType>().Cast<int>().Max() || type < EnumUtils<BanHandler.BanType>.Values.ToArray<BanHandler.BanType>().Cast<int>().Min())
		{
			return BanHandler.BanType.UserId;
		}
		return (BanHandler.BanType)type;
	}

	public static void Init()
	{
		try
		{
			if (!File.Exists(BanHandler.GetPath(BanHandler.BanType.UserId)))
			{
				File.Create(BanHandler.GetPath(BanHandler.BanType.UserId)).Close();
			}
			else
			{
				FileManager.RemoveEmptyLines(BanHandler.GetPath(BanHandler.BanType.UserId));
			}
			if (!File.Exists(BanHandler.GetPath(BanHandler.BanType.IP)))
			{
				File.Create(BanHandler.GetPath(BanHandler.BanType.IP)).Close();
			}
			else
			{
				FileManager.RemoveEmptyLines(BanHandler.GetPath(BanHandler.BanType.IP));
			}
		}
		catch
		{
			ServerConsole.AddLog("Can't create ban files!", ConsoleColor.Gray, false);
		}
		BanHandler.ValidateBans();
	}

	public static bool IssueBan(BanDetails ban, BanHandler.BanType banType, bool forced = false)
	{
		bool flag;
		try
		{
			if (banType == BanHandler.BanType.IP && ban.Id.Equals("localClient", StringComparison.OrdinalIgnoreCase))
			{
				flag = false;
			}
			else
			{
				ban.OriginalName = ban.OriginalName.Replace(";", ":");
				ban.Issuer = ban.Issuer.Replace(";", ":");
				ban.Reason = ban.Reason.Replace(";", ":");
				Misc.ReplaceUnsafeCharacters(ref ban.OriginalName, '?');
				Misc.ReplaceUnsafeCharacters(ref ban.Issuer, '?');
				List<BanDetails> bans = BanHandler.GetBans(banType);
				if (!bans.Any((BanDetails b) => b.Id == ban.Id))
				{
					BanIssuingEventArgs banIssuingEventArgs = new BanIssuingEventArgs(banType, ban);
					ServerEvents.OnBanIssuing(banIssuingEventArgs);
					if (!forced && !banIssuingEventArgs.IsAllowed)
					{
						return false;
					}
					banType = banIssuingEventArgs.BanType;
					ban = banIssuingEventArgs.BanDetails;
					FileManager.AppendFile(ban.ToString(), BanHandler.GetPath(banType), true);
					FileManager.RemoveEmptyLines(BanHandler.GetPath(banType));
					ServerEvents.OnBanIssued(new BanIssuedEventArgs(banType, ban));
				}
				else
				{
					BanDetails banDetails = bans.First((BanDetails old) => string.Equals(old.Id, ban.Id, StringComparison.OrdinalIgnoreCase));
					BanUpdatingEventArgs banUpdatingEventArgs = new BanUpdatingEventArgs(banType, ban, banDetails);
					ServerEvents.OnBanUpdating(banUpdatingEventArgs);
					if (!forced && !banUpdatingEventArgs.IsAllowed)
					{
						return false;
					}
					BanHandler.RemoveBan(ban.Id, banType, true);
					banType = banUpdatingEventArgs.BanType;
					ban = banUpdatingEventArgs.BanDetails;
					BanHandler.IssueBan(ban, banType, true);
					ServerEvents.OnBanUpdated(new BanUpdatedEventArgs(banType, ban, banDetails));
				}
				flag = true;
			}
		}
		catch
		{
			flag = false;
		}
		return flag;
	}

	public static void ValidateBans()
	{
		ServerConsole.AddLog("Validating bans...", ConsoleColor.Gray, false);
		BanHandler.ValidateBans(BanHandler.BanType.UserId);
		BanHandler.ValidateBans(BanHandler.BanType.IP);
		ServerConsole.AddLog("Bans has been validated.", ConsoleColor.Gray, false);
	}

	public static void ValidateBans(BanHandler.BanType banType)
	{
		List<string> list = FileManager.ReadAllLinesList(BanHandler.GetPath(banType));
		List<int> list2 = ListPool<int>.Shared.Rent();
		for (int i = list.Count - 1; i >= 0; i--)
		{
			string text = list[i];
			if (BanHandler.ProcessBanItem(text, banType) == null || !BanHandler.CheckExpiration(BanHandler.ProcessBanItem(text, banType), BanHandler.BanType.NULL))
			{
				list2.Add(i);
			}
		}
		List<int> list3 = ListPool<int>.Shared.Rent();
		foreach (int num in list2)
		{
			if (!list3.Contains(num))
			{
				list3.Add(num);
			}
		}
		ListPool<int>.Shared.Return(list2);
		foreach (int num2 in list3.OrderByDescending((int index) => index))
		{
			list.RemoveAt(num2);
		}
		ListPool<int>.Shared.Return(list3);
		if (FileManager.ReadAllLines(BanHandler.GetPath(banType)) == list.ToArray())
		{
			return;
		}
		FileManager.WriteToFile(list.ToArray(), BanHandler.GetPath(banType), false);
	}

	public static bool CheckExpiration(BanDetails ban, BanHandler.BanType banType)
	{
		if (ban == null)
		{
			return false;
		}
		if (TimeBehaviour.ValidateTimestamp(ban.Expires, TimeBehaviour.CurrentTimestamp(), 0L))
		{
			return true;
		}
		if (banType >= BanHandler.BanType.UserId)
		{
			BanHandler.RemoveBan(ban.Id, banType, true);
		}
		return false;
	}

	public static BanDetails ReturnChecks(BanDetails ban, BanHandler.BanType banType)
	{
		if (!BanHandler.CheckExpiration(ban, banType))
		{
			return null;
		}
		return ban;
	}

	public static void RemoveBan(string id, BanHandler.BanType banType, bool forced = false)
	{
		BanRevokingEventArgs banRevokingEventArgs = new BanRevokingEventArgs(banType, BanHandler.GetBan(id, banType));
		ServerEvents.OnBanRevoking(banRevokingEventArgs);
		if (!forced && !banRevokingEventArgs.IsAllowed)
		{
			return;
		}
		id = id.Replace(";", ":").Replace(Environment.NewLine, "").Replace("\n", "");
		FileManager.WriteToFile((from l in FileManager.ReadAllLines(BanHandler.GetPath(banType))
			where BanHandler.ProcessBanItem(l, banType) != null && BanHandler.ProcessBanItem(l, banType).Id != id
			select l).ToArray<string>(), BanHandler.GetPath(banType), false);
		ServerEvents.OnBanRevoked(new BanRevokedEventArgs(banType, banRevokingEventArgs.BanDetails));
	}

	public static List<BanDetails> GetBans(BanHandler.BanType banType)
	{
		return (from b in FileManager.ReadAllLines(BanHandler.GetPath(banType))
			select BanHandler.ProcessBanItem(b, banType) into b
			where b != null
			select b).ToList<BanDetails>();
	}

	public static BanDetails GetBan(string id, BanHandler.BanType banType)
	{
		return BanHandler.GetBans(banType).FirstOrDefault((BanDetails s) => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
	}

	public static KeyValuePair<BanDetails, BanDetails> QueryBan(string userId, string ip)
	{
		string text = null;
		string text2 = null;
		if (!string.IsNullOrEmpty(userId))
		{
			userId = userId.Replace(";", ":").Replace(Environment.NewLine, "").Replace("\n", "");
			text = FileManager.ReadAllLines(BanHandler.GetPath(BanHandler.BanType.UserId)).FirstOrDefault(delegate(string b)
			{
				BanDetails banDetails = BanHandler.ProcessBanItem(b, BanHandler.BanType.UserId);
				return ((banDetails != null) ? banDetails.Id : null) == userId;
			});
		}
		if (!string.IsNullOrEmpty(ip))
		{
			ip = ip.Replace(";", ":").Replace(Environment.NewLine, "").Replace("\n", "");
			text2 = FileManager.ReadAllLines(BanHandler.GetPath(BanHandler.BanType.IP)).FirstOrDefault(delegate(string b)
			{
				BanDetails banDetails2 = BanHandler.ProcessBanItem(b, BanHandler.BanType.IP);
				return ((banDetails2 != null) ? banDetails2.Id : null) == ip;
			});
		}
		return new KeyValuePair<BanDetails, BanDetails>(BanHandler.ReturnChecks(BanHandler.ProcessBanItem(text, BanHandler.BanType.UserId), BanHandler.BanType.UserId), BanHandler.ReturnChecks(BanHandler.ProcessBanItem(text2, BanHandler.BanType.IP), BanHandler.BanType.IP));
	}

	public static BanDetails ProcessBanItem(string ban, BanHandler.BanType banType)
	{
		if (string.IsNullOrEmpty(ban) || !ban.Contains(";"))
		{
			return null;
		}
		string[] array = ban.Split(';', StringSplitOptions.None);
		if (array.Length != 6)
		{
			return null;
		}
		if (banType == BanHandler.BanType.UserId && !array[1].Contains("@"))
		{
			array[1] = array[1].Trim() + "@steam";
		}
		return new BanDetails
		{
			OriginalName = array[0],
			Id = array[1].Trim(),
			Expires = Convert.ToInt64(array[2].Trim()),
			Reason = array[3],
			Issuer = array[4],
			IssuanceTime = Convert.ToInt64(array[5].Trim())
		};
	}

	public static string GetPath(BanHandler.BanType banType)
	{
		if (banType != BanHandler.BanType.UserId && banType == BanHandler.BanType.IP)
		{
			return ConfigSharing.Paths[0] + "IpBans.txt";
		}
		return ConfigSharing.Paths[0] + "UserIdBans.txt";
	}

	public enum BanType
	{
		NULL = -1,
		UserId,
		IP
	}
}
