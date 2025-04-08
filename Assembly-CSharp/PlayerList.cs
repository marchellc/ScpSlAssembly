using System;
using System.Collections.Generic;
using GameCore;
using MEC;
using Mirror;
using ToggleableMenus;
using UnityEngine;
using Utils.ConfigHandler;

public class PlayerList : SimpleToggleableMenu
{
	private static string ServerName
	{
		get
		{
			ServerConfigSynchronizer serverConfigSynchronizer = ServerConfigSynchronizer.Singleton;
			if (!(serverConfigSynchronizer == null))
			{
				return serverConfigSynchronizer.ServerName;
			}
			return null;
		}
		set
		{
			if (ServerConfigSynchronizer.Singleton == null)
			{
				return;
			}
			ServerConfigSynchronizer.Singleton.NetworkServerName = value;
		}
	}

	private void Update()
	{
		RectTransform component = base.GetComponent<RectTransform>();
		component.localPosition = Vector3.zero;
		component.sizeDelta = Vector2.zero;
	}

	private void Start()
	{
		PlayerList._anyAdminOnServer = false;
		if (!NetworkServer.active)
		{
			return;
		}
		ConfigFile.ServerConfig.UpdateConfigValue(PlayerList.RefreshRate);
		ConfigFile.ServerConfig.UpdateConfigValue(PlayerList.Title);
		Timing.RunCoroutine(this._RefreshTitleLoop(), Segment.FixedUpdate);
	}

	protected override void Awake()
	{
		base.Awake();
		PlayerList.instances.Clear();
		PlayerList.singleton = this;
		PlayerList.s_parent = this.parent;
		PlayerList.s_template = this.template;
	}

	public static void UpdatePlayerNickname(ReferenceHub instance)
	{
		foreach (PlayerList.Instance instance2 in PlayerList.instances)
		{
			if (!(instance2.owner == null) && !(instance2.owner != instance))
			{
				ReferenceHub hub = ReferenceHub.GetHub(instance2.owner);
				if (instance2.listElementReference != null && hub != null)
				{
					instance2.listElementReference.TextNick.text = hub.nicknameSync.DisplayName;
					break;
				}
				Debug.LogWarning("UpdatePlayerNickname: PlayerList Instance either has a null list element or is updating for an unknown player.");
				break;
			}
		}
	}

	public static void UpdatePlayerRole(ReferenceHub instance)
	{
		PlayerList._anyAdminOnServer = false;
		bool flag = instance == null;
		foreach (PlayerList.Instance instance2 in PlayerList.instances)
		{
			try
			{
				if (instance2 != null)
				{
					if (!PlayerList._anyAdminOnServer && !string.IsNullOrEmpty(instance.serverRoles.GetUncoloredRoleString()))
					{
						PlayerList._anyAdminOnServer = true;
					}
					if (!flag)
					{
						instance != instance2.owner;
					}
				}
			}
			catch (Exception ex)
			{
				global::GameCore.Console.AddLog("Exception caught (UpdatePlayerRole in PlayerList): " + ex.Message, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
				Debug.LogError("Exception caught (UpdatePlayerRole in PlayerList): " + ex.Message);
			}
		}
	}

	public void RefreshTitleSafe()
	{
		if (string.IsNullOrEmpty(PlayerList.Title.Value))
		{
			PlayerList.ServerName = ServerConsole.Singleton.RefreshServerNameSafe();
			return;
		}
		string text;
		if (!ServerConsole.Singleton.NameFormatter.TryProcessExpression(PlayerList.Title.Value, "player list title", out text))
		{
			ServerConsole.AddLog(text, ConsoleColor.Gray, false);
			return;
		}
		PlayerList.ServerName = text;
	}

	public void RefreshTitle()
	{
		PlayerList.ServerName = (string.IsNullOrEmpty(PlayerList.Title.Value) ? ServerConsole.Singleton.RefreshServerName() : ServerConsole.Singleton.NameFormatter.ProcessExpression(PlayerList.Title.Value));
	}

	private IEnumerator<float> _RefreshTitleLoop()
	{
		while (this != null)
		{
			this.RefreshTitleSafe();
			ushort i = 0;
			while ((float)i < 50f * PlayerList.RefreshRate.Value)
			{
				yield return 0f;
				ushort num = i;
				i = num + 1;
			}
		}
		yield break;
	}

	public static readonly ConfigEntry<float> RefreshRate = new ConfigEntry<float>("player_list_title_rate", 5f, "Player List Title Refresh Rate", "The amount of time (in seconds) between refreshing the title of the player list");

	public static readonly ConfigEntry<string> Title = new ConfigEntry<string>("player_list_title", null, "Player List Title", "The title at the top of the player list menu.");

	public Transform parent;

	public Transform template;

	public GameObject mainPanel;

	public GameObject reportForm;

	public GameObject reportPopup;

	public static InterfaceColorAdjuster ica;

	public static PlayerList singleton;

	private int _timer;

	private static Transform s_parent;

	private static Transform s_template;

	private static bool _anyAdminOnServer;

	public static readonly List<PlayerList.Instance> instances = new List<PlayerList.Instance>();

	[Serializable]
	public class Instance
	{
		public ReferenceHub owner;

		public PlayerListElement listElementReference;
	}
}
