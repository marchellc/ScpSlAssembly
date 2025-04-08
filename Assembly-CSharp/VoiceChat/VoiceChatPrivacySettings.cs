using System;
using System.Collections.Generic;
using InventorySystem;
using Mirror;
using ToggleableMenus;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceChat
{
	public class VoiceChatPrivacySettings : ToggleableMenuBase
	{
		public static VoiceChatPrivacySettings Singleton { get; private set; }

		public static event Action<ReferenceHub> OnUserFlagsChanged;

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

		public override bool CanToggle
		{
			get
			{
				return false;
			}
		}

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
				if (VoiceChatPrivacySettings._loadedFlags == value)
				{
					return;
				}
				VoiceChatPrivacySettings._loadedFlags = value;
				VoiceChatPrivacySettings.PrefsFlags = value;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			foreach (VoiceChatPrivacySettings.ToggleGroup toggleGroup in this._groups)
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
			foreach (VoiceChatPrivacySettings.ToggleGroup toggleGroup in this._groups)
			{
				toggleGroup.IsAccepted = (VoiceChatPrivacySettings.PrivacyFlags & toggleGroup.Flags) == toggleGroup.Flags;
			}
			this._recordDim.enabled = (VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.AllowRecording) == VcPrivacyFlags.None;
		}

		public void HandleToggle(Toggle checkbox)
		{
			foreach (VoiceChatPrivacySettings.ToggleGroup toggleGroup in this._groups)
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
			foreach (VoiceChatPrivacySettings.ToggleGroup toggleGroup2 in this._groups)
			{
				if (toggleGroup2.IsAccepted)
				{
					vcPrivacyFlags |= toggleGroup2.Flags;
				}
			}
			VoiceChatPrivacySettings.PrivacyFlags = vcPrivacyFlags;
			ReferenceHub referenceHub;
			if (!NetworkClient.ready || !ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			NetworkClient.Send<VoiceChatPrivacySettings.VcPrivacyMessage>(new VoiceChatPrivacySettings.VcPrivacyMessage
			{
				Flags = (byte)VoiceChatPrivacySettings.PrivacyFlags
			}, 0);
			Action<ReferenceHub> onUserFlagsChanged = VoiceChatPrivacySettings.OnUserFlagsChanged;
			if (onUserFlagsChanged == null)
			{
				return;
			}
			onUserFlagsChanged(referenceHub);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Inventory.OnServerStarted += delegate
			{
				NetworkServer.ReplaceHandler<VoiceChatPrivacySettings.VcPrivacyMessage>(delegate(NetworkConnectionToClient conn, VoiceChatPrivacySettings.VcPrivacyMessage msg)
				{
					ReferenceHub referenceHub;
					if (!ReferenceHub.TryGetHub(conn, out referenceHub))
					{
						return;
					}
					VoiceChatPrivacySettings.FlagsOfPlayers[referenceHub] = (VcPrivacyFlags)msg.Flags;
					Action<ReferenceHub> onUserFlagsChanged = VoiceChatPrivacySettings.OnUserFlagsChanged;
					if (onUserFlagsChanged == null)
					{
						return;
					}
					onUserFlagsChanged(referenceHub);
				}, true);
			};
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				if (!hub.isLocalPlayer || NetworkServer.active)
				{
					return;
				}
				NetworkClient.Send<VoiceChatPrivacySettings.VcPrivacyMessage>(new VoiceChatPrivacySettings.VcPrivacyMessage
				{
					Flags = (byte)VoiceChatPrivacySettings.PrivacyFlags
				}, 0);
			}));
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				VoiceChatPrivacySettings.FlagsOfPlayers.Remove(hub);
			}));
		}

		public static bool CheckUserFlags(ReferenceHub user, VcPrivacyFlags flags)
		{
			VcPrivacyFlags vcPrivacyFlags;
			return VoiceChatPrivacySettings.FlagsOfPlayers.TryGetValue(user, out vcPrivacyFlags) && (vcPrivacyFlags & flags) == flags;
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
		private VoiceChatPrivacySettings.ToggleGroup[] _groups;

		private readonly Dictionary<VcPrivacyFlags, VoiceChatPrivacySettings.ToggleGroup> _groupsByFlags = new Dictionary<VcPrivacyFlags, VoiceChatPrivacySettings.ToggleGroup>();

		private static readonly Dictionary<ReferenceHub, VcPrivacyFlags> FlagsOfPlayers = new Dictionary<ReferenceHub, VcPrivacyFlags>();

		private const string PrefsKey = "VcPrivacyFlags_1.1";

		private bool _forceOpen;

		private static VcPrivacyFlags _loadedFlags;

		private static bool _flagsLoaded;

		[Serializable]
		private class ToggleGroup
		{
			public Toggle AcceptToggle { get; private set; }

			public Toggle DenyToggle { get; private set; }

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
	}
}
