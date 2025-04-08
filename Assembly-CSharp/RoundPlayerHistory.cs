using System;
using System.Collections.Generic;
using UnityEngine;

public class RoundPlayerHistory : MonoBehaviour
{
	private void Awake()
	{
		RoundPlayerHistory.singleton = this;
	}

	public RoundPlayerHistory.PlayerHistoryLog GetData(int playerId)
	{
		foreach (RoundPlayerHistory.PlayerHistoryLog playerHistoryLog in this.historyLogs)
		{
			if (playerHistoryLog.PlayerId == playerId)
			{
				return playerHistoryLog;
			}
		}
		return null;
	}

	public void SetData(int playerId, string newNick, int newPlayerId, string newUserId, int newConnectionStatus, int newAliveClass, int newCurrentClass, DateTime newStartTime, DateTime newStopTime)
	{
		int num = -1;
		if (playerId == -1)
		{
			this.historyLogs.Add(new RoundPlayerHistory.PlayerHistoryLog
			{
				Nickname = "Player",
				PlayerId = 0,
				UserId = string.Empty,
				ConnectionStatus = 0,
				LastAliveClass = -1,
				CurrentClass = -1,
				ConnectionStart = DateTime.Now,
				ConnectionStop = new DateTime(0, 0, 0)
			});
			num = this.historyLogs.Count - 1;
		}
		else
		{
			for (int i = 0; i < this.historyLogs.Count; i++)
			{
				if (this.historyLogs[i].PlayerId == playerId)
				{
					num = i;
				}
			}
		}
		if (num < 0)
		{
			return;
		}
		if (newNick != string.Empty)
		{
			this.historyLogs[num].Nickname = newNick;
		}
		if (newPlayerId != 0)
		{
			this.historyLogs[num].PlayerId = newPlayerId;
		}
		if (newUserId != string.Empty)
		{
			this.historyLogs[num].UserId = newUserId;
		}
		if (newConnectionStatus != 0)
		{
			this.historyLogs[num].ConnectionStatus = newConnectionStatus;
		}
		if (newAliveClass != 0)
		{
			this.historyLogs[num].LastAliveClass = newAliveClass;
		}
		if (newCurrentClass != 0)
		{
			this.historyLogs[num].CurrentClass = newCurrentClass;
		}
		if (newStartTime.Year != 0)
		{
			this.historyLogs[num].ConnectionStart = newStartTime;
		}
		if (newStopTime.Year != 0)
		{
			this.historyLogs[num].ConnectionStop = newStopTime;
		}
	}

	public static RoundPlayerHistory singleton;

	public List<RoundPlayerHistory.PlayerHistoryLog> historyLogs = new List<RoundPlayerHistory.PlayerHistoryLog>();

	[Serializable]
	public class PlayerHistoryLog
	{
		public string Nickname;

		public int PlayerId;

		public string UserId;

		public int ConnectionStatus;

		public int LastAliveClass;

		public int CurrentClass;

		public DateTime ConnectionStart;

		public DateTime ConnectionStop;
	}
}
