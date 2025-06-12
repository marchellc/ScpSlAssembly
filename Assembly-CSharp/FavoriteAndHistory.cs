using System;
using System.Collections.Generic;
using System.IO;
using GameCore;
using UnityEngine;

public class FavoriteAndHistory : MonoBehaviour
{
	public enum StorageLocation
	{
		History,
		Favorites,
		IPHistory
	}

	public const int MaxHistoryAmount = 10;

	public static readonly List<string> Favorites;

	public static readonly List<string> History;

	public static readonly List<string> IPHistory;

	public static string ServerIDLastJoined;

	public static readonly Dictionary<StorageLocation, List<string>> LocationToList;

	private static readonly Dictionary<StorageLocation, string> StorageEnumToPath;

	public static string GetPath(StorageLocation location)
	{
		return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/" + FavoriteAndHistory.StorageEnumToPath[location];
	}

	public static void ResetServerID()
	{
		FavoriteAndHistory.ServerIDLastJoined = string.Empty;
	}

	static FavoriteAndHistory()
	{
		FavoriteAndHistory.Favorites = new List<string>();
		FavoriteAndHistory.History = new List<string>();
		FavoriteAndHistory.IPHistory = new List<string>();
		FavoriteAndHistory.LocationToList = new Dictionary<StorageLocation, List<string>>
		{
			{
				StorageLocation.History,
				FavoriteAndHistory.History
			},
			{
				StorageLocation.Favorites,
				FavoriteAndHistory.Favorites
			},
			{
				StorageLocation.IPHistory,
				FavoriteAndHistory.IPHistory
			}
		};
		FavoriteAndHistory.StorageEnumToPath = new Dictionary<StorageLocation, string>
		{
			{
				StorageLocation.History,
				"history.txt"
			},
			{
				StorageLocation.Favorites,
				"favorites.txt"
			},
			{
				StorageLocation.IPHistory,
				"iphistory.txt"
			}
		};
		foreach (KeyValuePair<StorageLocation, List<string>> locationTo in FavoriteAndHistory.LocationToList)
		{
			FavoriteAndHistory.Load(locationTo.Key);
		}
	}

	public static void Load(StorageLocation location)
	{
		GameCore.Console.AddLog("Loading " + location, Color.grey);
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

	public static void Modify(StorageLocation location, string id, bool delete = false)
	{
		List<string> list = FavoriteAndHistory.LocationToList[location];
		list.RemoveAll((string x) => x == id);
		if (!delete)
		{
			list.Add(id);
		}
		StreamWriter streamWriter = new StreamWriter(FavoriteAndHistory.GetPath(location), append: false);
		foreach (string item in list)
		{
			streamWriter.WriteLine(item);
		}
		streamWriter.Close();
		FavoriteAndHistory.Load(location);
		if ((location == StorageLocation.History || location == StorageLocation.IPHistory) && list.Count >= 10)
		{
			FavoriteAndHistory.HistoryLimit(location, id);
		}
	}

	private static void HistoryLimit(StorageLocation location, string id)
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
		StreamWriter streamWriter = new StreamWriter(FavoriteAndHistory.GetPath(location), append: false);
		foreach (string item in list)
		{
			streamWriter.WriteLine(item);
		}
		streamWriter.Close();
		FavoriteAndHistory.Load(location);
	}

	public static bool IsInStorage(StorageLocation location, string id)
	{
		return FavoriteAndHistory.LocationToList[location].Contains(id);
	}

	private static void Revert(string fileName)
	{
		Debug.Log("Reverting:" + fileName);
		new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/" + fileName).Close();
	}
}
