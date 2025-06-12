using System;
using GameObjectPools;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173BlinkTimer : SubroutineBase, IPoolResettable
{
	private const float CooldownBaseline = 3f;

	private const float CooldownPerObserver = 0f;

	private const float BreakneckCooldownMultiplier = 0.5f;

	public const float SustainTime = 2f;

	private Scp173ObserversTracker _observers;

	private Scp173MovementModule _fpcModule;

	private Scp173BreakneckSpeedsAbility _breakneckSpeedsAbility;

	private float _totalCooldown;

	private double _initialStopTime;

	private double _endSustainTime;

	private float TotalCooldown
	{
		get
		{
			if (!NetworkServer.active)
			{
				return this._totalCooldown;
			}
			return this.TotalCooldownServer;
		}
	}

	private float TotalCooldownServer => this._totalCooldown * (this._breakneckSpeedsAbility.IsActive ? 0.5f : 1f);

	private float RemainingSustain => (float)(this._endSustainTime - NetworkTime.time);

	public float RemainingBlinkCooldown => Mathf.Max(0f, (float)(this._initialStopTime + (double)this.TotalCooldown - NetworkTime.time));

	public float RemainingSustainPercent
	{
		get
		{
			if (this._endSustainTime != -1.0)
			{
				return Mathf.Clamp(this.RemainingSustain, 0f, 2f) / 2f;
			}
			return 1f;
		}
	}

	public bool AbilityReady
	{
		get
		{
			if (this.RemainingSustainPercent > 0f)
			{
				return this.RemainingBlinkCooldown <= 0f;
			}
			return false;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (base.Role is Scp173Role scp173Role)
		{
			this._fpcModule = scp173Role.FpcModule as Scp173MovementModule;
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeedsAbility);
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observers);
			this._observers.OnObserversChanged += OnObserversChanged;
			Scp173BreakneckSpeedsAbility breakneckSpeedsAbility = this._breakneckSpeedsAbility;
			breakneckSpeedsAbility.OnToggled = (Action)Delegate.Combine(breakneckSpeedsAbility.OnToggled, (Action)delegate
			{
				base.ServerSendRpc(toAll: true);
			});
		}
	}

	private void OnObserversChanged(int prev, int current)
	{
		if (NetworkServer.active)
		{
			if (prev == 0 && this.RemainingSustainPercent == 0f)
			{
				this._initialStopTime = NetworkTime.time;
				this._totalCooldown = 3f;
			}
			this._totalCooldown += 0f * (float)(current - prev);
			this._endSustainTime = ((current > 0) ? (-1.0) : (NetworkTime.time + 2.0));
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteDouble(this._initialStopTime);
		writer.WriteDouble(this._endSustainTime);
		writer.WriteFloat(this.TotalCooldownServer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (!NetworkServer.active)
		{
			this._initialStopTime = reader.ReadDouble();
			this._endSustainTime = reader.ReadDouble();
			this._totalCooldown = reader.ReadFloat();
		}
	}

	public void ResetObject()
	{
		this._totalCooldown = 0f;
		this._initialStopTime = 0.0;
		this._endSustainTime = 0.0;
	}

	public void ServerBlink(Vector3 pos)
	{
		int currentObservers = this._observers.CurrentObservers;
		this._fpcModule.ServerTeleportTo(pos);
		this._initialStopTime = NetworkTime.time;
		this._observers.UpdateObservers();
		if (currentObservers == this._observers.CurrentObservers)
		{
			base.ServerSendRpc(toAll: true);
		}
	}
}
