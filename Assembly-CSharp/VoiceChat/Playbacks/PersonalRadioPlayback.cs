using System;
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

namespace VoiceChat.Playbacks
{
	public class PersonalRadioPlayback : VoiceChatPlaybackBase, IGlobalPlayback
	{
		private int RangeId
		{
			get
			{
				RadioStatusMessage radioStatusMessage;
				if (!RadioMessages.SyncedRangeLevels.TryGetValue(this._owner.netId, out radioStatusMessage))
				{
					return 1;
				}
				return Mathf.Abs((int)radioStatusMessage.Range);
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
				RadioItem radioItem;
				if (!InventoryItemLoader.TryGetItem<RadioItem>(ItemType.Radio, out radioItem))
				{
					return null;
				}
				PersonalRadioPlayback._templateRadioLoaded = true;
				PersonalRadioPlayback._templateRadio = radioItem;
				return radioItem;
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
					return;
				}
				PersonalRadioPlayback._localPlayer = value;
				PersonalRadioPlayback._hasLocalPlayer = true;
				value._isLocalPlayer = true;
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
				RadioItem radioItem;
				return this.TryGetUserRadio(out radioItem) && radioItem.IsUsable;
			}
		}

		public override int MaxSamples
		{
			get
			{
				return this._personalBuffer.Length;
			}
		}

		public bool GlobalChatActive
		{
			get
			{
				return PersonalRadioPlayback.IsTransmitting(this._owner) && !base.Source.mute;
			}
		}

		public Color GlobalChatColor
		{
			get
			{
				return this._owner.serverRoles.GetVoiceColor();
			}
		}

		public string GlobalChatName
		{
			get
			{
				return this._owner.nicknameSync.DisplayName;
			}
		}

		public float GlobalChatLoudness
		{
			get
			{
				return base.Loudness;
			}
		}

		public GlobalChatIconType GlobalChatIcon
		{
			get
			{
				return GlobalChatIconType.Radio;
			}
		}

		private void OnItemsModified(ReferenceHub hub)
		{
			if (hub != this._owner)
			{
				return;
			}
			this._recheckCachedRadio = true;
		}

		private void UpdateTemporaryId()
		{
			if (this._personalBuffer.Length == 0)
			{
				if (this._currentId == 0)
				{
					return;
				}
				PersonalRadioPlayback.FreeIds.Add(this._currentId);
				PersonalRadioPlayback._freeIdsCount++;
				this._currentId = 0;
				return;
			}
			else
			{
				if (this._currentId != 0)
				{
					return;
				}
				if (PersonalRadioPlayback._freeIdsCount > 0)
				{
					this._currentId = PersonalRadioPlayback.FreeIds.Min();
					PersonalRadioPlayback._freeIdsCount--;
					return;
				}
				this._currentId = ++PersonalRadioPlayback._lastTopNumber;
				return;
			}
		}

		private void UpdateLoudness()
		{
			if (!PersonalRadioPlayback._hasLocalPlayer || this._isLocalPlayer || !PersonalRadioPlayback.LocalPlayer.RadioUsable)
			{
				base.Source.mute = true;
				if (this._hasProximity)
				{
					this._proxPlayback.Source.volume = 1f;
				}
				return;
			}
			int num = Mathf.Max(PersonalRadioPlayback._localPlayer.RangeId, this.RangeId);
			float num2 = (float)this.RadioTemplate.Ranges[num].MaximumRange;
			float sqrMagnitude = (MainCameraController.CurrentCamera.position - this.LastKnownLocation).sqrMagnitude;
			if (sqrMagnitude > num2 * num2)
			{
				base.Source.mute = true;
				return;
			}
			base.Source.mute = false;
			float num3 = Mathf.Sqrt(sqrMagnitude) / num2;
			base.Source.volume = this.RadioTemplate.VoiceVolumeCurve.Evaluate(num3);
			if (this._personalBuffer.Length > 0)
			{
				PersonalRadioPlayback._noiseLevel = Mathf.Max(PersonalRadioPlayback._noiseLevel, this.RadioTemplate.NoiseLevelCurve.Evaluate(num3));
			}
			if (this._hasProximity)
			{
				this._proxPlayback.Source.volume = ((this._personalBuffer.Length > 0) ? 0.35f : 1f);
			}
		}

		private void UpdateNoise()
		{
			if (!this._isLocalPlayer)
			{
				return;
			}
			this._noiseSource.volume = PersonalRadioPlayback._noiseLevel;
			PersonalRadioPlayback._noiseLevel = 0f;
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
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in this._owner.inventory.UserInventory.Items)
			{
				if (keyValuePair.Value.ItemTypeId == ItemType.Radio)
				{
					radio = (RadioItem)keyValuePair.Value;
					break;
				}
			}
			if (radio == null)
			{
				return false;
			}
			this._cachedRadio = radio;
			return true;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Inventory.OnItemsModified -= this.OnItemsModified;
			GlobalChatIndicatorManager.Unsubscribe(this);
			if (!this._isLocalPlayer)
			{
				return;
			}
			PersonalRadioPlayback.LocalPlayer = null;
			this._noiseSource.volume = 0f;
		}

		protected override void Update()
		{
			base.Update();
			this.UpdateTemporaryId();
			this.UpdateLoudness();
			this.UpdateNoise();
			if (!NetworkServer.active || !PersonalRadioPlayback.IsTransmitting(this._owner))
			{
				return;
			}
			NetworkServer.SendToReady<PersonalRadioPlayback.TransmitterPositionMessage>(new PersonalRadioPlayback.TransmitterPositionMessage
			{
				Transmitter = new RecyclablePlayerId(this._owner.PlayerId),
				WaypointId = new RelativePosition(base.transform.position).WaypointId
			}, 0);
		}

		protected override float ReadSample()
		{
			return this._personalBuffer.Read();
		}

		public void Setup(ReferenceHub owner, SingleBufferPlayback proximityPlayback)
		{
			this._owner = owner;
			this._proxPlayback = proximityPlayback;
			this._personalBuffer.Clear();
			Inventory.OnItemsModified += this.OnItemsModified;
			if (this._owner.isLocalPlayer)
			{
				PersonalRadioPlayback.LocalPlayer = this;
			}
			else
			{
				this._isLocalPlayer = false;
				GlobalChatIndicatorManager.Subscribe(this, owner);
			}
			this._hasProximity = this._proxPlayback != null;
			this._recheckCachedRadio = true;
		}

		public void DistributeSamples(float[] samples, int length)
		{
			this._personalBuffer.Write(samples, length);
			if (this._hasProximity)
			{
				this._personalBuffer.SyncWith(this._proxPlayback.Buffer, 4000);
			}
			int num = this.TemporaryId - 1;
			if (num < 0 || num >= 8)
			{
				return;
			}
			foreach (SpatializedRadioPlaybackBase spatializedRadioPlaybackBase in SpatializedRadioPlaybackBase.AllInstances)
			{
				if (spatializedRadioPlaybackBase.IgnoredNetId != this._owner.netId && !spatializedRadioPlaybackBase.Culled)
				{
					float num2 = (float)this.RadioTemplate.Ranges[Mathf.Max(spatializedRadioPlaybackBase.RangeId, this.RangeId)].MaximumRange;
					if ((spatializedRadioPlaybackBase.LastPosition - this.LastKnownLocation).sqrMagnitude <= num2 * num2)
					{
						PlaybackBuffer playbackBuffer = spatializedRadioPlaybackBase.Buffers[num];
						playbackBuffer.Write(samples, length);
						if (this._hasProximity)
						{
							playbackBuffer.SyncWith(this._proxPlayback.Buffer, 4000);
						}
					}
				}
			}
		}

		public static bool IsTransmitting(ReferenceHub hub)
		{
			IVoiceRole voiceRole = hub.roleManager.CurrentRole as IVoiceRole;
			if (voiceRole == null)
			{
				return false;
			}
			VoiceModuleBase voiceModule = voiceRole.VoiceModule;
			if (!(voiceModule is IRadioVoiceModule))
			{
				return false;
			}
			if (hub.isLocalPlayer)
			{
				return VoiceChatMicCapture.GetCurrentChannel() == VoiceChatChannel.Radio;
			}
			return (NetworkServer.active ? voiceModule.ServerIsSending : voiceModule.IsSpeaking) && voiceModule.CurrentChannel == VoiceChatChannel.Radio;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<PersonalRadioPlayback.TransmitterPositionMessage>(delegate(PersonalRadioPlayback.TransmitterPositionMessage msg)
				{
					ReferenceHub referenceHub;
					if (!ReferenceHub.TryGetHub(msg.Transmitter.Value, out referenceHub))
					{
						return;
					}
					IVoiceRole voiceRole = referenceHub.roleManager.CurrentRole as IVoiceRole;
					if (voiceRole == null)
					{
						return;
					}
					IRadioVoiceModule radioVoiceModule = voiceRole.VoiceModule as IRadioVoiceModule;
					if (radioVoiceModule == null)
					{
						return;
					}
					radioVoiceModule.RadioPlayback.LastKnownLocation = WaypointBase.GetWorldPosition(msg.WaypointId, Vector3.zero);
				}, true);
			};
		}

		[SerializeField]
		private AudioSource _noiseSource;

		private int _currentId;

		private bool _hasProximity;

		private bool _isLocalPlayer;

		private bool _recheckCachedRadio;

		private ReferenceHub _owner;

		private RadioItem _cachedRadio;

		private SingleBufferPlayback _proxPlayback;

		private readonly PlaybackBuffer _personalBuffer = new PlaybackBuffer(24000, false);

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

		public struct TransmitterPositionMessage : NetworkMessage
		{
			public RecyclablePlayerId Transmitter;

			public byte WaypointId;
		}
	}
}
