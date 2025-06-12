using System;
using System.Diagnostics;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096StateController : StandardSubroutine<Scp096Role>
{
	private Scp096RageState _rageState;

	private Scp096AbilityState _abilityState;

	private readonly Stopwatch _rageChangeSw = new Stopwatch();

	private readonly Stopwatch _abilityChangeSw = new Stopwatch();

	public Scp096RageState RageState
	{
		get
		{
			return this._rageState;
		}
		set
		{
			if (this._rageState != value)
			{
				this.OnRageUpdate?.Invoke(value);
				this._rageState = value;
				this._rageChangeSw.Restart();
				if (NetworkServer.active)
				{
					base.ServerSendRpc(toAll: true);
				}
			}
		}
	}

	public Scp096AbilityState AbilityState
	{
		get
		{
			return this._abilityState;
		}
		set
		{
			if (this._abilityState != value)
			{
				this.OnAbilityUpdate?.Invoke(value);
				this._abilityState = value;
				this._abilityChangeSw.Restart();
				if (NetworkServer.active)
				{
					base.ServerSendRpc(toAll: true);
				}
			}
		}
	}

	public float LastRageUpdate => (float)this._rageChangeSw.Elapsed.TotalSeconds;

	public float LastAbilityUpdate => (float)this._abilityChangeSw.Elapsed.TotalSeconds;

	public event Action<Scp096RageState> OnRageUpdate;

	public event Action<Scp096AbilityState> OnAbilityUpdate;

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this.RageState);
		writer.WriteByte((byte)this.AbilityState);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (!NetworkServer.active)
		{
			this.RageState = (Scp096RageState)reader.ReadByte();
			this.AbilityState = (Scp096AbilityState)reader.ReadByte();
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.RageState = Scp096RageState.Docile;
		this.AbilityState = Scp096AbilityState.None;
		this._rageChangeSw.Stop();
		this._abilityChangeSw.Stop();
		ReferenceHub.OnPlayerAdded -= OnPlayerJoin;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		this._rageChangeSw.Start();
		this._abilityChangeSw.Start();
		ReferenceHub.OnPlayerAdded += OnPlayerJoin;
	}

	private void OnPlayerJoin(ReferenceHub hub)
	{
		if (NetworkServer.active)
		{
			base.ServerSendRpc(hub);
		}
	}

	public void SetRageState(Scp096RageState state)
	{
		Scp096ChangingStateEventArgs e = new Scp096ChangingStateEventArgs(base.Owner, state);
		Scp096Events.OnChangingState(e);
		if (e.IsAllowed)
		{
			state = e.State;
			this.RageState = state;
			Scp096Events.OnChangedState(new Scp096ChangedStateEventArgs(base.Owner, state));
		}
	}

	public void SetAbilityState(Scp096AbilityState state)
	{
		this.AbilityState = state;
	}
}
