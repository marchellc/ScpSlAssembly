using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114StrangleAudio : StandardSubroutine<Scp3114Role>
{
	private enum RpcType
	{
		ChokeSync,
		Kill
	}

	private Scp3114Strangle _strangle;

	private bool _isChoking;

	private RpcType _rpcType;

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

	private bool LocallyStrangled
	{
		get
		{
			if (!ReferenceHub.TryGetPovHub(out var hub))
			{
				return false;
			}
			Strangled effect = hub.playerEffectsController.GetEffect<Strangled>();
			if (effect.IsEnabled && effect.TryUpdateAttacker(out var attacker))
			{
				return attacker == base.Owner;
			}
			return false;
		}
	}

	private void ServerSendRpc(RpcType rpcType)
	{
		this._rpcType = rpcType;
		base.ServerSendRpc(toAll: true);
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.UpdateServer();
		}
		this._chokeSource.mute = !base.Role.IsLocalPlayer && !this.LocallyStrangled;
		this._chokeSource.volume = Mathf.MoveTowards(this._chokeSource.volume, this._isChoking ? 1 : 0, Time.deltaTime * this._volumeAdjustSpeed);
	}

	private void UpdateServer()
	{
		if (!this._strangle.SyncTarget.HasValue)
		{
			if (this._isChoking)
			{
				this._isChoking = false;
				this.ServerSendRpc(RpcType.ChokeSync);
			}
		}
		else if (this._syncCooldown.IsReady)
		{
			Strangled effect = this._strangle.SyncTarget.Value.Target.playerEffectsController.GetEffect<Strangled>();
			this._isChoking = true;
			this._syncKillTime = NetworkTime.time + (double)effect.EstimatedTimeToKill;
			this.ServerSendRpc(RpcType.ChokeSync);
			this._syncCooldown.Trigger(0.20000000298023224);
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
		if (!(num3 < this._minPitchAdjust))
		{
			if (num3 > this._maxPitchAdjust)
			{
				double num4 = (double)this._killEventTimeSeconds - num;
				this._chokeSource.timeSamples = (int)(num4 * this._secondsToSamples);
				this._chokeSource.pitch = 1f;
			}
			else
			{
				this._chokeSource.pitch = num2;
			}
		}
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp3114Strangle>(out this._strangle);
		this._secondsToSamples = (float)this._chokeSource.clip.samples / this._chokeSource.clip.length;
		this._samplesToSeconds = 1.0 / this._secondsToSamples;
		this._strangle.ServerOnKill += delegate
		{
			this.ServerSendRpc(RpcType.Kill);
		};
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._rpcType);
		switch (this._rpcType)
		{
		case RpcType.Kill:
			writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
			break;
		case RpcType.ChokeSync:
			writer.WriteDouble(this._isChoking ? this._syncKillTime : 0.0);
			break;
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.Kill:
			AudioSourcePoolManager.PlayAtPosition(this._killSoundClip, reader.ReadRelativePosition(), this._killSoundRange, 1f, FalloffType.Exponential, MixerChannel.NoDucking);
			break;
		case RpcType.ChokeSync:
			this._syncKillTime = reader.ReadDouble();
			this._isChoking = this._syncKillTime != 0.0;
			this.ResyncAudio();
			break;
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
}
