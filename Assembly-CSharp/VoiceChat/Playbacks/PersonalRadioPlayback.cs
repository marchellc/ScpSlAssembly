using System.Collections.Generic;
using System.Linq;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Radio;
using Mirror;
using PlayerRoles.Voice;
using RelativePositioning;
using UnityEngine;
using VoiceChat.Networking;

namespace VoiceChat.Playbacks;

public class PersonalRadioPlayback : VoiceChatPlaybackBase, IGlobalPlayback
{
	public struct TransmitterPositionMessage : NetworkMessage
	{
		public RecyclablePlayerId Transmitter;

		public byte WaypointId;
	}

	[SerializeField]
	private AudioSource _noiseSource;

	private int _currentId;

	private bool _hasProximity;

	private bool _isLocalPlayer;

	private bool _recheckCachedRadio;

	private ReferenceHub _owner;

	private RadioItem _cachedRadio;

	private SingleBufferPlayback[] _proxPlaybacks;

	private readonly PlaybackBuffer _personalBuffer = new PlaybackBuffer();

	private const int RadioDelay = 4000;

	private const float ProxVolumeRatio = 0.35f;

	private static PersonalRadioPlayback _localPlayer;

	private static bool _hasLocalPlayer;

	private static int _freeIdsCount;

	private static int _lastTopNumber;

	private static float _noiseLevel;

	private static RadioItem _templateRadio;

	private static bool _templateRadioLoaded;

	private static readonly HashSet<int> FreeIds = new HashSet<int>();

	private int RangeId
	{
		get
		{
			if (!RadioMessages.SyncedRangeLevels.TryGetValue(this._owner.netId, out var value))
			{
				return 1;
			}
			return Mathf.Abs((int)value.Range);
		}
	}

	private RadioItem RadioTemplate
	{
		get
		{
			if (PersonalRadioPlayback._templateRadioLoaded)
			{
				return PersonalRadioPlayback._templateRadio;
			}
			if (!InventoryItemLoader.TryGetItem<RadioItem>(ItemType.Radio, out var result))
			{
				return null;
			}
			PersonalRadioPlayback._templateRadioLoaded = true;
			PersonalRadioPlayback._templateRadio = result;
			return result;
		}
	}

	private static PersonalRadioPlayback LocalPlayer
	{
		get
		{
			return PersonalRadioPlayback._localPlayer;
		}
		set
		{
			if (PersonalRadioPlayback._hasLocalPlayer)
			{
				PersonalRadioPlayback._localPlayer._isLocalPlayer = false;
			}
			if (value == null)
			{
				PersonalRadioPlayback._localPlayer = null;
				PersonalRadioPlayback._hasLocalPlayer = false;
			}
			else
			{
				PersonalRadioPlayback._localPlayer = value;
				PersonalRadioPlayback._hasLocalPlayer = true;
				value._isLocalPlayer = true;
			}
		}
	}

	public Vector3 LastKnownLocation { get; private set; }

	public int TemporaryId
	{
		get
		{
			this.UpdateTemporaryId();
			return this._currentId;
		}
	}

	public bool RadioUsable
	{
		get
		{
			if (this.TryGetUserRadio(out var radio))
			{
				return radio.IsUsable;
			}
			return false;
		}
	}

	public override int MaxSamples => this._personalBuffer.Length;

	public bool GlobalChatActive
	{
		get
		{
			if (PersonalRadioPlayback.IsTransmitting(this._owner))
			{
				return !base.Source.mute;
			}
			return false;
		}
	}

	public Color GlobalChatColor => this._owner.serverRoles.GetVoiceColor();

	public string GlobalChatName => this._owner.nicknameSync.DisplayName;

	public float GlobalChatLoudness => base.Loudness;

	public GlobalChatIconType GlobalChatIcon => GlobalChatIconType.Radio;

	private void OnItemsModified(ReferenceHub hub)
	{
		if (!(hub != this._owner))
		{
			this._recheckCachedRadio = true;
		}
	}

	private void UpdateTemporaryId()
	{
		if (this._personalBuffer.Length == 0)
		{
			if (this._currentId != 0)
			{
				PersonalRadioPlayback.FreeIds.Add(this._currentId);
				PersonalRadioPlayback._freeIdsCount++;
				this._currentId = 0;
			}
		}
		else if (this._currentId == 0)
		{
			if (PersonalRadioPlayback._freeIdsCount > 0)
			{
				this._currentId = PersonalRadioPlayback.FreeIds.Min();
				PersonalRadioPlayback._freeIdsCount--;
			}
			else
			{
				this._currentId = ++PersonalRadioPlayback._lastTopNumber;
			}
		}
	}

	private void UpdateLoudness()
	{
		if (!PersonalRadioPlayback._hasLocalPlayer || this._isLocalPlayer || !PersonalRadioPlayback.LocalPlayer.RadioUsable)
		{
			base.Source.mute = true;
			if (this._hasProximity)
			{
				this._proxPlaybacks.ForEach(delegate(SingleBufferPlayback x)
				{
					x.Source.volume = 1f;
				});
			}
			return;
		}
		int num = Mathf.Max(PersonalRadioPlayback._localPlayer.RangeId, this.RangeId);
		RadioRangeMode radioRangeMode = this.RadioTemplate.Ranges[num];
		if (!radioRangeMode.CheckRange(MainCameraController.LastPosition, this.LastKnownLocation, out var sqrMag))
		{
			base.Source.mute = true;
			return;
		}
		base.Source.mute = false;
		float time = Mathf.Sqrt(sqrMag) / (float)radioRangeMode.MaximumRange;
		base.Source.volume = this.RadioTemplate.VoiceVolumeCurve.Evaluate(time);
		if (this._personalBuffer.Length > 0)
		{
			PersonalRadioPlayback._noiseLevel = Mathf.Max(PersonalRadioPlayback._noiseLevel, this.RadioTemplate.NoiseLevelCurve.Evaluate(time));
		}
		if (this._hasProximity)
		{
			SingleBufferPlayback[] proxPlaybacks = this._proxPlaybacks;
			for (int num2 = 0; num2 < proxPlaybacks.Length; num2++)
			{
				proxPlaybacks[num2].Source.volume = ((this._personalBuffer.Length > 0) ? 0.35f : 1f);
			}
		}
	}

	private void UpdateNoise()
	{
		if (this._isLocalPlayer)
		{
			this._noiseSource.volume = PersonalRadioPlayback._noiseLevel;
			PersonalRadioPlayback._noiseLevel = 0f;
		}
	}

	private bool TryGetUserRadio(out RadioItem radio)
	{
		if (this._cachedRadio != null)
		{
			radio = this._cachedRadio;
			return true;
		}
		radio = null;
		if (!this._recheckCachedRadio)
		{
			return false;
		}
		foreach (KeyValuePair<ushort, ItemBase> item in this._owner.inventory.UserInventory.Items)
		{
			if (item.Value.ItemTypeId == ItemType.Radio)
			{
				radio = (RadioItem)item.Value;
				break;
			}
		}
		if ((object)radio == null)
		{
			return false;
		}
		this._cachedRadio = radio;
		return true;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		Inventory.OnItemsModified -= OnItemsModified;
		GlobalChatIndicatorManager.Unsubscribe(this);
		if (this._isLocalPlayer)
		{
			PersonalRadioPlayback.LocalPlayer = null;
			this._noiseSource.volume = 0f;
		}
	}

	protected override void Update()
	{
		base.Update();
		this.UpdateTemporaryId();
		this.UpdateLoudness();
		this.UpdateNoise();
		if (NetworkServer.active && PersonalRadioPlayback.IsTransmitting(this._owner))
		{
			NetworkServer.SendToReady(new TransmitterPositionMessage
			{
				Transmitter = new RecyclablePlayerId(this._owner),
				WaypointId = new RelativePosition(base.transform.position).WaypointId
			});
		}
	}

	protected override float ReadSample()
	{
		return this._personalBuffer.Read();
	}

	public void Setup(ReferenceHub owner, SingleBufferPlayback[] proximityPlaybacks)
	{
		this._owner = owner;
		this._proxPlaybacks = proximityPlaybacks;
		this._personalBuffer.Clear();
		Inventory.OnItemsModified += OnItemsModified;
		if (this._owner.isLocalPlayer)
		{
			PersonalRadioPlayback.LocalPlayer = this;
		}
		else
		{
			this._isLocalPlayer = false;
			GlobalChatIndicatorManager.Subscribe(this, owner);
		}
		this._hasProximity = this._proxPlaybacks != null && this._proxPlaybacks.Length != 0;
		this._recheckCachedRadio = true;
	}

	public void DistributeSamples(float[] samples, int length)
	{
		this._personalBuffer.Write(samples, length);
		if (this._hasProximity)
		{
			this._personalBuffer.SyncWith(this._proxPlaybacks[0].Buffer, 4000);
		}
		int num = this.TemporaryId - 1;
		if (num < 0 || num >= 8)
		{
			return;
		}
		foreach (SpatializedRadioPlaybackBase allInstance in SpatializedRadioPlaybackBase.AllInstances)
		{
			if (allInstance.IgnoredNetId == this._owner.netId || allInstance.Culled)
			{
				continue;
			}
			RadioRangeMode radioRangeMode = this.RadioTemplate.Ranges[Mathf.Max(allInstance.RangeId, this.RangeId)];
			if (radioRangeMode.CheckRange(allInstance.LastPosition, this.LastKnownLocation, out var _))
			{
				PlaybackBuffer playbackBuffer = allInstance.Buffers[num];
				playbackBuffer.Write(samples, length);
				if (this._hasProximity)
				{
					playbackBuffer.SyncWith(this._proxPlaybacks[0].Buffer, 4000);
				}
			}
		}
	}

	public static bool IsTransmitting(ReferenceHub hub)
	{
		if (!(hub.roleManager.CurrentRole is IVoiceRole { VoiceModule: var voiceModule }))
		{
			return false;
		}
		if (!(voiceModule is IRadioVoiceModule))
		{
			return false;
		}
		if (hub.isLocalPlayer)
		{
			return VoiceChatMicCapture.GetCurrentChannel() == VoiceChatChannel.Radio;
		}
		if (NetworkServer.active ? voiceModule.ServerIsSending : voiceModule.IsSpeaking)
		{
			return voiceModule.CurrentChannel == VoiceChatChannel.Radio;
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler(delegate(TransmitterPositionMessage msg)
			{
				if (ReferenceHub.TryGetHub(msg.Transmitter.Value, out var hub) && hub.roleManager.CurrentRole is IVoiceRole { VoiceModule: IRadioVoiceModule voiceModule })
				{
					voiceModule.RadioPlayback.LastKnownLocation = WaypointBase.GetWorldPosition(msg.WaypointId, Vector3.zero);
				}
			});
		};
	}
}
