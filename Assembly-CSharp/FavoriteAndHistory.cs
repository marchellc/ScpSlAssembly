using System;
using System.Collections.Generic;
using System.IO;
using GameCore;
using UnityEngine;

public class FavoriteAndHistory : MonoBehaviour
{
	public static string GetPath(FavoriteAndHistory.StorageLocation location)
	{
		return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/" + FavoriteAndHistory.StorageEnumToPath[location];
	}

	public static void ResetServerID()
	{
		FavoriteAndHistory.ServerIDLastJoined = string.Empty;
	}

	static FavoriteAndHistory()
	{
		foreach (KeyValuePair<FavoriteAndHistory.StorageLocation, List<string>> keyValuePair in FavoriteAndHistory.LocationToList)
		{
			FavoriteAndHistory.Load(keyValuePair.Key);
		}
	}

	public static void Load(FavoriteAndHistory.StorageLocation location)
	{
		global::GameCore.Console.AddLog("Loading " + location.ToString(), Color.grey, false, global::GameCore.Console.ConsoleLogType.Log);
		try
		{
			List<string> list = FavoriteAndHistory.LocationToList[location];
			list.Clear();
			if (!File.Exists(FavoriteAndHistory.GetPath(location)))
			{
				FavoriteAndHistory.Revert(FavoriteAndHistory.StorageEnumToPath[location]);
			}
			StreamReader streamReader = new StreamReader(FavoriteAndHistory.GetPath(location));
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				if (!string.IsNullOrEmpty(text) && !list.Contains(text))
				{
					list.Add(text);
				}
			}
			streamReader.Close();
		}
		catch (Exception ex)
		{
			Debug.Log("REVENT: " + ex.StackTrace + " - " + ex.Message);
			FavoriteAndHistory.Revert(FavoriteAndHistory.StorageEnumToPath[location]);
		}
	}

	public static void Modify(FavoriteAndHistory.StorageLocation location, string id, bool delete = false)
	{
		List<string> list = FavoriteAndHistory.LocationToList[location];
		list.RemoveAll((string x) => x == id);
		if (!delete)
		{
			list.Add(id);
		}
		StreamWriter streamWriter = new StreamWriter(FavoriteAndHistory.GetPath(location), false);
		foreach (string text in list)
		{
			streamWriter.WriteLine(text);
		}
		streamWriter.Close();
		FavoriteAndHistory.Load(location);
		if ((location == FavoriteAndHistory.StorageLocation.History || location == FavoriteAndHistory.StorageLocation.IPHistory) && list.Count >= 10)
		{
			FavoriteAndHistory.HistoryLimit(location, id);
		}
	}

	private static void HistoryLimit(FavoriteAndHistory.StorageLocation location, string id)
	{
		int num = 0;
		List<string> list = FavoriteAndHistory.LocationToList[location];
		if (list.Contains(id))
		{
			num = list.RemoveAll((string x) => x == id);
		}
		if (num == 0)
		{
			list.RemoveAt(0);
		}
		list.Add(id);
		StreamWriter streamWriter = new StreamWriter(FavoriteAndHistory.GetPath(location), false);
		foreach (string text in list)
		{
			streamWriter.WriteLine(text);
		}
		streamWriter.Close();
		FavoriteAndHistory.Load(location);
	}

	public static bool IsInStorage(FavoriteAndHistory.StorageLocation location, string id)
	{
		return FavoriteAndHistory.LocationToList[location].Contains(id);
	}

	private static void Revert(string fileName)
	{
		Debug.Log("Reverting:" + fileName);
		new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/" + fileName).Close();
	}

	public const int MaxHistoryAmount = 10;

	public static readonly List<string> Favorites = new List<string>();

	public static readonly List<string> History = new List<string>();

	public static readonly List<string> IPHistory = new List<string>();

	public static string ServerIDLastJoined;

	public static readonly Dictionary<FavoriteAndHistory.StorageLocation, List<string>> LocationToList = new Dictionary<FavoriteAndHistory.StorageLocation, List<string>>
	{
		{
			FavoriteAndHistory.StorageLocation.History,
			FavoriteAndHistory.History
		},
		{
			FavoriteAndHistory.StorageLocation.Favorites,
			FavoriteAndHistory.Favorites
		},
		{
			FavoriteAndHistory.StorageLocation.IPHistory,
			FavoriteAndHistory.IPHistory
		}
	};

	private static readonly Dictionary<FavoriteAndHistory.StorageLocation, string> StorageEnumToPath = new Dictionary<FavoriteAndHistory.StorageLocation, string>
	{
		{
			FavoriteAndHistory.StorageLocation.History,
			"history.txt"
		},
		{
			FavoriteAndHistory.StorageLocation.Favorites,
			"favorites.txt"
		},
		{
			FavoriteAndHistory.StorageLocation.IPHistory,
			"iphistory.txt"
		}
	};

	public enum StorageLocation
	{
		History,
		Favorites,
		IPHistory
	}
}
