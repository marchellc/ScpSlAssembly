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
	[Serializable]
	public class Instance
	{
		public ReferenceHub owner;

		public PlayerListElement listElementReference;
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

	public static readonly List<Instance> instances = new List<Instance>();

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
			if (!(ServerConfigSynchronizer.Singleton == null))
			{
				ServerConfigSynchronizer.Singleton.NetworkServerName = value;
			}
		}
	}

	private void Update()
	{
		RectTransform component = GetComponent<RectTransform>();
		component.localPosition = Vector3.zero;
		component.sizeDelta = Vector2.zero;
	}

	private void Start()
	{
		_anyAdminOnServer = false;
		if (NetworkServer.active)
		{
			ConfigFile.ServerConfig.UpdateConfigValue(RefreshRate);
			ConfigFile.ServerConfig.UpdateConfigValue(Title);
			Timing.RunCoroutine(_RefreshTitleLoop(), Segment.FixedUpdate);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		instances.Clear();
		singleton = this;
		s_parent = parent;
		s_template = template;
	}

	public static void UpdatePlayerNickname(ReferenceHub instance)
	{
		foreach (Instance instance2 in instances)
		{
			if (!(instance2.owner == null) && !(instance2.owner != instance))
			{
				ReferenceHub hub = ReferenceHub.GetHub(instance2.owner);
				if (instance2.listElementReference != null && hub != null)
				{
					instance2.listElementReference.TextNick.text = hub.nicknameSync.DisplayName;
				}
				else
				{
					Debug.LogWarning("UpdatePlayerNickname: PlayerList Instance either has a null list element or is updating for an unknown player.");
				}
				break;
			}
		}
	}

	public static void UpdatePlayerRole(ReferenceHub instance)
	{
		_anyAdminOnServer = false;
		bool flag = instance == null;
		foreach (Instance instance2 in instances)
		{
			try
			{
				if (instance2 != null)
				{
					if (!_anyAdminOnServer && !string.IsNullOrEmpty(instance.serverRoles.GetUncoloredRoleString()))
					{
						_anyAdminOnServer = true;
					}
					if (!flag)
					{
						_ = instance != instance2.owner;
					}
				}
			}
			catch (Exception ex)
			{
				GameCore.Console.AddLog("Exception caught (UpdatePlayerRole in PlayerList): " + ex.Message, Color.red);
				Debug.LogError("Exception caught (UpdatePlayerRole in PlayerList): " + ex.Message);
			}
		}
	}

	public void RefreshTitleSafe()
	{
		string result;
		if (string.IsNullOrEmpty(Title.Value))
		{
			ServerName = ServerConsole.Singleton.RefreshServerNameSafe();
		}
		else if (!ServerConsole.Singleton.NameFormatter.TryProcessExpression(Title.Value, "player list title", out result))
		{
			ServerConsole.AddLog(result);
		}
		else
		{
			ServerName = result;
		}
	}

	public void RefreshTitle()
	{
		ServerName = (string.IsNullOrEmpty(Title.Value) ? ServerConsole.Singleton.RefreshServerName() : ServerConsole.Singleton.NameFormatter.ProcessExpression(Title.Value));
	}

	private IEnumerator<float> _RefreshTitleLoop()
	{
		while (this != null)
		{
			RefreshTitleSafe();
			ushort i = 0;
			while ((float)(int)i < 50f * RefreshRate.Value)
			{
				yield return 0f;
				i++;
			}
		}
	}
}
