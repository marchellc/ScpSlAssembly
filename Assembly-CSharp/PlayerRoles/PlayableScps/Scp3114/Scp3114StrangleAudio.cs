using System;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114StrangleAudio : StandardSubroutine<Scp3114Role>
	{
		private bool LocallyStrangled
		{
			get
			{
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetPovHub(out referenceHub))
				{
					return false;
				}
				Strangled effect = referenceHub.playerEffectsController.GetEffect<Strangled>();
				ReferenceHub referenceHub2;
				return effect.IsEnabled && effect.TryUpdateAttacker(out referenceHub2) && referenceHub2 == base.Owner;
			}
		}

		private void ServerSendRpc(Scp3114StrangleAudio.RpcType rpcType)
		{
			this._rpcType = rpcType;
			base.ServerSendRpc(true);
		}

		private void Update()
		{
			if (NetworkServer.active)
			{
				this.UpdateServer();
			}
			this._chokeSource.mute = !base.Role.IsLocalPlayer && !this.LocallyStrangled;
			this._chokeSource.volume = Mathf.MoveTowards(this._chokeSource.volume, (float)(this._isChoking ? 1 : 0), Time.deltaTime * this._volumeAdjustSpeed);
		}

		private void UpdateServer()
		{
			if (this._strangle.SyncTarget == null)
			{
				if (!this._isChoking)
				{
					return;
				}
				this._isChoking = false;
				this.ServerSendRpc(Scp3114StrangleAudio.RpcType.ChokeSync);
				return;
			}
			else
			{
				if (!this._syncCooldown.IsReady)
				{
					return;
				}
				Strangled effect = this._strangle.SyncTarget.Value.Target.playerEffectsController.GetEffect<Strangled>();
				this._isChoking = true;
				this._syncKillTime = NetworkTime.time + (double)effect.EstimatedTimeToKill;
				this.ServerSendRpc(Scp3114StrangleAudio.RpcType.ChokeSync);
				this._syncCooldown.Trigger(0.20000000298023224);
				return;
			}
		}

		private void ResyncAudio()
		{
			double num = this._syncKillTime - NetworkTime.time;
			if (num <= 0.0)
			{
				return;
			}
			if (!this._chokeSource.isPlaying)
			{
				this._chokeSource.Play();
			}
			float num2 = (float)(((double)this._killEventTimeSeconds - (double)this._chokeSource.timeSamples * this._samplesToSeconds) / num);
			float num3 = Mathf.Abs(1f - num2);
			if (num3 < this._minPitchAdjust)
			{
				return;
			}
			if (num3 > this._maxPitchAdjust)
			{
				double num4 = (double)this._killEventTimeSeconds - num;
				this._chokeSource.timeSamples = (int)(num4 * this._secondsToSamples);
				this._chokeSource.pitch = 1f;
				return;
			}
			this._chokeSource.pitch = num2;
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp3114Strangle>(out this._strangle);
			this._secondsToSamples = (double)((float)this._chokeSource.clip.samples / this._chokeSource.clip.length);
			this._samplesToSeconds = 1.0 / this._secondsToSamples;
			this._strangle.ServerOnKill += delegate
			{
				this.ServerSendRpc(Scp3114StrangleAudio.RpcType.Kill);
			};
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this._rpcType);
			Scp3114StrangleAudio.RpcType rpcType = this._rpcType;
			if (rpcType != Scp3114StrangleAudio.RpcType.ChokeSync)
			{
				if (rpcType == Scp3114StrangleAudio.RpcType.Kill)
				{
					writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
					return;
				}
			}
			else
			{
				writer.WriteDouble(this._isChoking ? this._syncKillTime : 0.0);
			}
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			Scp3114StrangleAudio.RpcType rpcType = (Scp3114StrangleAudio.RpcType)reader.ReadByte();
			if (rpcType != Scp3114StrangleAudio.RpcType.ChokeSync)
			{
				if (rpcType == Scp3114StrangleAudio.RpcType.Kill)
				{
					AudioSourcePoolManager.PlayAtPosition(this._killSoundClip, reader.ReadRelativePosition(), this._killSoundRange, 1f, FalloffType.Exponential, MixerChannel.NoDucking, 1f);
					return;
				}
			}
			else
			{
				this._syncKillTime = reader.ReadDouble();
				this._isChoking = this._syncKillTime != 0.0;
				this.ResyncAudio();
			}
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._syncCooldown.Clear();
			this._chokeSource.Stop();
			this._isChoking = false;
			this._chokeSource.volume = 0f;
			this._chokeSource.mute = true;
		}

		private Scp3114Strangle _strangle;

		private bool _isChoking;

		private Scp3114StrangleAudio.RpcType _rpcType;

		private double _syncKillTime;

		private readonly AbilityCooldown _syncCooldown = new AbilityCooldown();

		[SerializeField]
		private AudioSource _chokeSource;

		[SerializeField]
		private AudioClip _killSoundClip;

		[SerializeField]
		private float _killSoundRange;

		[SerializeField]
		private float _volumeAdjustSpeed;

		[SerializeField]
		private float _killEventTimeSeconds;

		[SerializeField]
		private float _minPitchAdjust;

		[SerializeField]
		private float _maxPitchAdjust;

		private const float MinSyncCooldownSeconds = 0.2f;

		private double _samplesToSeconds;

		private double _secondsToSamples;

		private enum RpcType
		{
			ChokeSync,
			Kill
		}
	}
}
