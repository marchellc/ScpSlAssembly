using System;
using System.Diagnostics;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096StateController : StandardSubroutine<Scp096Role>
	{
		public event Action<Scp096RageState> OnRageUpdate;

		public event Action<Scp096AbilityState> OnAbilityUpdate;

		public Scp096RageState RageState
		{
			get
			{
				return this._rageState;
			}
			set
			{
				if (this._rageState == value)
				{
					return;
				}
				Action<Scp096RageState> onRageUpdate = this.OnRageUpdate;
				if (onRageUpdate != null)
				{
					onRageUpdate(value);
				}
				this._rageState = value;
				this._rageChangeSw.Restart();
				if (NetworkServer.active)
				{
					base.ServerSendRpc(true);
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
				if (this._abilityState == value)
				{
					return;
				}
				Action<Scp096AbilityState> onAbilityUpdate = this.OnAbilityUpdate;
				if (onAbilityUpdate != null)
				{
					onAbilityUpdate(value);
				}
				this._abilityState = value;
				this._abilityChangeSw.Restart();
				if (NetworkServer.active)
				{
					base.ServerSendRpc(true);
				}
			}
		}

		public float LastRageUpdate
		{
			get
			{
				return (float)this._rageChangeSw.Elapsed.TotalSeconds;
			}
		}

		public float LastAbilityUpdate
		{
			get
			{
				return (float)this._abilityChangeSw.Elapsed.TotalSeconds;
			}
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this.RageState);
			writer.WriteByte((byte)this.AbilityState);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (NetworkServer.active)
			{
				return;
			}
			this.RageState = (Scp096RageState)reader.ReadByte();
			this.AbilityState = (Scp096AbilityState)reader.ReadByte();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.RageState = Scp096RageState.Docile;
			this.AbilityState = Scp096AbilityState.None;
			this._rageChangeSw.Stop();
			this._abilityChangeSw.Stop();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._rageChangeSw.Start();
			this._abilityChangeSw.Start();
		}

		public void SetRageState(Scp096RageState state)
		{
			Scp096ChangingStateEventArgs scp096ChangingStateEventArgs = new Scp096ChangingStateEventArgs(base.Owner, state);
			Scp096Events.OnChangingState(scp096ChangingStateEventArgs);
			if (!scp096ChangingStateEventArgs.IsAllowed)
			{
				return;
			}
			state = scp096ChangingStateEventArgs.State;
			this.RageState = state;
			Scp096Events.OnChangedState(new Scp096ChangedStateEventArgs(base.Owner, state));
		}

		public void SetAbilityState(Scp096AbilityState state)
		{
			this.AbilityState = state;
		}

		private Scp096RageState _rageState;

		private Scp096AbilityState _abilityState;

		private readonly Stopwatch _rageChangeSw = new Stopwatch();

		private readonly Stopwatch _abilityChangeSw = new Stopwatch();
	}
}
