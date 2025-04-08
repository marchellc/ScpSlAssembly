using System;
using GameObjectPools;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173BlinkTimer : SubroutineBase, IPoolResettable
	{
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

		private float TotalCooldownServer
		{
			get
			{
				return this._totalCooldown * (this._breakneckSpeedsAbility.IsActive ? 0.5f : 1f);
			}
		}

		private float RemainingSustain
		{
			get
			{
				return (float)(this._endSustainTime - NetworkTime.time);
			}
		}

		public float RemainingBlinkCooldown
		{
			get
			{
				return Mathf.Max(0f, (float)(this._initialStopTime + (double)this.TotalCooldown - NetworkTime.time));
			}
		}

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
				return this.RemainingSustainPercent > 0f && this.RemainingBlinkCooldown <= 0f;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			Scp173Role scp173Role = base.Role as Scp173Role;
			if (scp173Role == null)
			{
				return;
			}
			this._fpcModule = scp173Role.FpcModule as Scp173MovementModule;
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeedsAbility);
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observers);
			this._observers.OnObserversChanged += this.OnObserversChanged;
			Scp173BreakneckSpeedsAbility breakneckSpeedsAbility = this._breakneckSpeedsAbility;
			breakneckSpeedsAbility.OnToggled = (Action)Delegate.Combine(breakneckSpeedsAbility.OnToggled, new Action(delegate
			{
				base.ServerSendRpc(true);
			}));
		}

		private void OnObserversChanged(int prev, int current)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (prev == 0 && this.RemainingSustainPercent == 0f)
			{
				this._initialStopTime = NetworkTime.time;
				this._totalCooldown = 3f;
			}
			this._totalCooldown += 0f * (float)(current - prev);
			this._endSustainTime = ((current > 0) ? (-1.0) : (NetworkTime.time + 2.0));
			base.ServerSendRpc(true);
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
				base.ServerSendRpc(true);
			}
		}

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
	}
}
