using System;
using System.Diagnostics;
using GameObjectPools;
using Mirror;
using UnityEngine;
using VoiceChat;
using VoiceChat.Codec;
using VoiceChat.Networking;

namespace PlayerRoles.Voice
{
	public abstract class VoiceModuleBase : MonoBehaviour, IPoolResettable, IPoolSpawnable
	{
		public event VoiceModuleBase.SamplesReceived OnSamplesReceived;

		protected ReferenceHub Owner
		{
			get
			{
				return this._owner;
			}
		}

		protected virtual OpusDecoder Decoder
		{
			get
			{
				return this._defaultDecoder;
			}
		}

		public PlayerRoleBase Role { get; private set; }

		public bool ServerIsSending { get; private set; }

		public GroupMuteFlags ReceiveFlags { get; set; }

		public VoiceChatChannel CurrentChannel
		{
			get
			{
				return this._lastChannel;
			}
			internal set
			{
				if (this._lastChannel == value)
				{
					return;
				}
				this._lastChannel = value;
				this.OnChannelChanged();
			}
		}

		public abstract bool IsSpeaking { get; }

		protected virtual void Awake()
		{
			this.Role = base.GetComponent<PlayerRoleBase>();
		}

		protected virtual void Update()
		{
			if (this._sentPackets > this._prevSent)
			{
				this.ServerIsSending = true;
				this._silenceStopwatch.Restart();
			}
			else if (this.ServerIsSending && this._silenceStopwatch.Elapsed.TotalSeconds > 0.10000000149011612)
			{
				this.ServerIsSending = false;
			}
			if (this._rateStopwatch.Elapsed.TotalSeconds >= 0.5)
			{
				this._sentPackets = 0;
				this._rateStopwatch.Restart();
			}
			this._prevSent = this._sentPackets;
		}

		protected virtual void OnChannelChanged()
		{
		}

		protected abstract void ProcessSamples(float[] data, int len);

		public abstract VoiceChatChannel GetUserInput();

		public virtual VoiceChatChannel ValidateSend(VoiceChatChannel channel)
		{
			return channel;
		}

		public virtual VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
		{
			return channel;
		}

		public virtual void ResetObject()
		{
			this._lastChannel = VoiceChatChannel.None;
			OpusDecoder defaultDecoder = this._defaultDecoder;
			if (defaultDecoder != null)
			{
				defaultDecoder.Dispose();
			}
			this.ReceiveFlags = GroupMuteFlags.None;
		}

		public virtual void SpawnObject()
		{
			if (!this.Role.TryGetOwner(out this._owner))
			{
				return;
			}
			this._defaultDecoder = new OpusDecoder();
			if (this.Owner.isLocalPlayer)
			{
				VoiceChatMicCapture.StartRecording();
			}
			if (NetworkServer.active)
			{
				this.ReceiveFlags = VoiceChatReceivePrefs.GetFlagsForUser(this.Owner);
			}
		}

		public bool CheckRateLimit()
		{
			int sentPackets = this._sentPackets;
			this._sentPackets = sentPackets + 1;
			return sentPackets < 128;
		}

		public void ProcessMessage(VoiceMessage msg)
		{
			this.CurrentChannel = msg.Channel;
			if (!VoiceModuleBase._receiveBufferSet)
			{
				VoiceModuleBase._receiveBufferSet = true;
				VoiceModuleBase._receiveBuffer = new float[24000];
			}
			int num = this.Decoder.Decode(msg.Data, msg.DataLength, VoiceModuleBase._receiveBuffer);
			if (!this.Owner.isLocalPlayer && VoiceChatMutes.GetFlags(this.Owner) != VcMuteFlags.None)
			{
				return;
			}
			this.ProcessSamples(VoiceModuleBase._receiveBuffer, num);
			VoiceModuleBase.SamplesReceived onSamplesReceived = this.OnSamplesReceived;
			if (onSamplesReceived == null)
			{
				return;
			}
			onSamplesReceived(VoiceModuleBase._receiveBuffer, num);
		}

		private VoiceChatChannel _lastChannel;

		private OpusDecoder _defaultDecoder;

		private ReferenceHub _owner;

		private int _sentPackets;

		private int _prevSent;

		private const float SilenceTolerance = 0.1f;

		private const float RateLimiterTimeframe = 0.5f;

		private const int RateLimiterTolerance = 128;

		private readonly Stopwatch _rateStopwatch = Stopwatch.StartNew();

		private readonly Stopwatch _silenceStopwatch = Stopwatch.StartNew();

		private static float[] _receiveBuffer;

		private static bool _receiveBufferSet;

		public delegate void SamplesReceived(float[] samples, int len);
	}
}
