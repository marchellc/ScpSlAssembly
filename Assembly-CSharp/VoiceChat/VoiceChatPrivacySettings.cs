using System;
using System.Collections.Generic;
using InventorySystem;
using Mirror;
using ToggleableMenus;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceChat;

public class VoiceChatPrivacySettings : ToggleableMenuBase
{
	[Serializable]
	private class ToggleGroup
	{
		[field: SerializeField]
		public Toggle AcceptToggle { get; private set; }

		[field: SerializeField]
		public Toggle DenyToggle { get; private set; }

		[field: SerializeField]
		public VcPrivacyFlags Flags { get; private set; }

		public bool IsAccepted
		{
			get
			{
				return this.AcceptToggle.isOn;
			}
			set
			{
				this.AcceptToggle.SetIsOnWithoutNotify(value);
				this.DenyToggle.SetIsOnWithoutNotify(!value);
			}
		}
	}

	public struct VcPrivacyMessage : NetworkMessage
	{
		public byte Flags;
	}

	[SerializeField]
	private GameObject _simplifiedRoot;

	[SerializeField]
	private GameObject _advancedRoot;

	[SerializeField]
	private Canvas _hideHudCanvas;

	[SerializeField]
	private Image _recordDim;

	[SerializeField]
	private Image _dimmerBackground;

	[SerializeField]
	private GameObject _returnButton;

	[SerializeField]
	private ToggleGroup[] _groups;

	private readonly Dictionary<VcPrivacyFlags, ToggleGroup> _groupsByFlags = new Dictionary<VcPrivacyFlags, ToggleGroup>();

	private static readonly Dictionary<ReferenceHub, VcPrivacyFlags> FlagsOfPlayers = new Dictionary<ReferenceHub, VcPrivacyFlags>();

	private const string PrefsKey = "VcPrivacyFlags_1.1";

	private bool _forceOpen;

	private static VcPrivacyFlags _loadedFlags;

	private static bool _flagsLoaded;

	public static VoiceChatPrivacySettings Singleton { get; private set; }

	public override bool IsEnabled
	{
		get
		{
			return base.IsEnabled;
		}
		set
		{
			base.IsEnabled = value || this._forceOpen;
		}
	}

	public override bool CanToggle => false;

	private static VcPrivacyFlags PrefsFlags
	{
		get
		{
			return (VcPrivacyFlags)PlayerPrefs.GetInt("VcPrivacyFlags_1.1", 0);
		}
		set
		{
			PlayerPrefs.SetInt("VcPrivacyFlags_1.1", (int)value);
		}
	}

	public static VcPrivacyFlags PrivacyFlags
	{
		get
		{
			if (VoiceChatPrivacySettings._flagsLoaded)
			{
				return VoiceChatPrivacySettings._loadedFlags;
			}
			VoiceChatPrivacySettings._loadedFlags = VoiceChatPrivacySettings.PrefsFlags;
			VoiceChatPrivacySettings._flagsLoaded = true;
			return VoiceChatPrivacySettings._loadedFlags;
		}
		set
		{
			if (VoiceChatPrivacySettings._loadedFlags != value)
			{
				VoiceChatPrivacySettings._loadedFlags = value;
				VoiceChatPrivacySettings.PrefsFlags = value;
			}
		}
	}

	public static event Action<ReferenceHub> OnUserFlagsChanged;

	protected override void Awake()
	{
		base.Awake();
		ToggleGroup[] groups = this._groups;
		foreach (ToggleGroup toggleGroup in groups)
		{
			this._groupsByFlags.Add(toggleGroup.Flags, toggleGroup);
		}
		VoiceChatPrivacySettings.Singleton = this;
		this.UpdateToggles();
	}

	protected override void OnToggled()
	{
	}

	public void UpdateToggles()
	{
		ToggleGroup[] groups = this._groups;
		foreach (ToggleGroup toggleGroup in groups)
		{
			toggleGroup.IsAccepted = (VoiceChatPrivacySettings.PrivacyFlags & toggleGroup.Flags) == toggleGroup.Flags;
		}
		this._recordDim.enabled = (VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.AllowRecording) == 0;
	}

	public void HandleToggle(Toggle checkbox)
	{
		ToggleGroup[] groups = this._groups;
		foreach (ToggleGroup toggleGroup in groups)
		{
			if (toggleGroup.AcceptToggle == checkbox)
			{
				toggleGroup.IsAccepted = true;
				break;
			}
			if (toggleGroup.DenyToggle == checkbox)
			{
				toggleGroup.IsAccepted = false;
				break;
			}
		}
		if (this._groupsByFlags[VcPrivacyFlags.AllowMicCapture].IsAccepted)
		{
			this._recordDim.enabled = false;
		}
		else
		{
			this._recordDim.enabled = true;
			this._groupsByFlags[VcPrivacyFlags.AllowRecording].IsAccepted = false;
		}
		VcPrivacyFlags vcPrivacyFlags = VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.SettingsSelected;
		groups = this._groups;
		foreach (ToggleGroup toggleGroup2 in groups)
		{
			if (toggleGroup2.IsAccepted)
			{
				vcPrivacyFlags |= toggleGroup2.Flags;
			}
		}
		VoiceChatPrivacySettings.PrivacyFlags = vcPrivacyFlags;
		if (NetworkClient.ready && ReferenceHub.TryGetLocalHub(out var hub))
		{
			NetworkClient.Send(new VcPrivacyMessage
			{
				Flags = (byte)VoiceChatPrivacySettings.PrivacyFlags
			});
			VoiceChatPrivacySettings.OnUserFlagsChanged?.Invoke(hub);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Inventory.OnServerStarted += delegate
		{
			NetworkServer.ReplaceHandler(delegate(NetworkConnectionToClient conn, VcPrivacyMessage msg)
			{
				if (ReferenceHub.TryGetHub(conn, out var hub))
				{
					VoiceChatPrivacySettings.FlagsOfPlayers[hub] = (VcPrivacyFlags)msg.Flags;
					VoiceChatPrivacySettings.OnUserFlagsChanged?.Invoke(hub);
				}
			});
		};
		ReferenceHub.OnPlayerAdded += delegate(ReferenceHub hub)
		{
			if (hub.isLocalPlayer && !NetworkServer.active)
			{
				NetworkClient.Send(new VcPrivacyMessage
				{
					Flags = (byte)VoiceChatPrivacySettings.PrivacyFlags
				});
			}
		};
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			VoiceChatPrivacySettings.FlagsOfPlayers.Remove(hub);
		};
	}

	public static bool CheckUserFlags(ReferenceHub user, VcPrivacyFlags flags)
	{
		if (VoiceChatPrivacySettings.FlagsOfPlayers.TryGetValue(user, out var value))
		{
			return (value & flags) == flags;
		}
		return false;
	}
}
