using System;
using System.Diagnostics;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173BreakneckSpeedsAbility : KeySubroutine<Scp173Role>
	{
		private float Elapsed
		{
			get
			{
				return (float)this._duration.Elapsed.TotalSeconds;
			}
		}

		public bool IsActive
		{
			get
			{
				return this._duration.IsRunning;
			}
			private set
			{
				if (value == this.IsActive)
				{
					return;
				}
				Scp173BreakneckSpeedChangingEventArgs scp173BreakneckSpeedChangingEventArgs = new Scp173BreakneckSpeedChangingEventArgs(base.Owner, value);
				Scp173Events.OnBreakneckSpeedChanging(scp173BreakneckSpeedChangingEventArgs);
				if (!scp173BreakneckSpeedChangingEventArgs.IsAllowed)
				{
					return;
				}
				if (value)
				{
					this._duration.Start();
					this._disableTime = 0f;
				}
				else
				{
					this._duration.Reset();
				}
				if (!NetworkServer.active)
				{
					return;
				}
				if (!value)
				{
					this.Cooldown.Trigger(40.0);
				}
				base.ServerSendRpc(true);
				Action onToggled = this.OnToggled;
				if (onToggled != null)
				{
					onToggled();
				}
				Scp173Events.OnBreakneckSpeedChanged(new Scp173BreakneckSpeedChangedEventArgs(base.Owner, value));
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Run;
			}
		}

		private void UpdateServerside()
		{
			if (!this.IsActive)
			{
				return;
			}
			if (this._disableTime > 0f)
			{
				if (this.Elapsed < this._disableTime)
				{
					return;
				}
				this.IsActive = false;
				return;
			}
			else
			{
				if (!this._observersTracker.IsObserved)
				{
					return;
				}
				this._disableTime = this.Elapsed + 10f;
				return;
			}
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			base.ClientSendCmd();
		}

		protected override void Update()
		{
			base.Update();
			if (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated())
			{
				this._ppVolume.enabled = true;
				this._ppVolume.weight = Mathf.Lerp(this._ppVolume.weight, (float)(this.IsActive ? 1 : 0), Time.deltaTime * this._ppLerpSpeed);
			}
			else
			{
				this._ppVolume.enabled = false;
			}
			if (NetworkServer.active)
			{
				this.UpdateServerside();
			}
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			if (this.IsActive)
			{
				if (this.Elapsed < 1f)
				{
					return;
				}
				this.IsActive = false;
				return;
			}
			else
			{
				if (!this.Cooldown.IsReady)
				{
					return;
				}
				this.IsActive = true;
				return;
			}
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			writer.WriteBool(this.IsActive);
			if (!this.IsActive)
			{
				this.Cooldown.WriteCooldown(writer);
			}
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			this.IsActive = reader.ReadBool();
			if (!this.IsActive)
			{
				this.Cooldown.ReadCooldown(reader);
			}
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._ppVolume.weight = 0f;
			this.IsActive = false;
			this.Cooldown.Clear();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this.Cooldown.Trigger(40.0);
		}

		private const float RechargeTime = 40f;

		private const float StareLimit = 10f;

		private const float MinimalTime = 1f;

		private readonly Stopwatch _duration = new Stopwatch();

		private float _disableTime;

		private Scp173ObserversTracker _observersTracker;

		[SerializeField]
		private Volume _ppVolume;

		[SerializeField]
		private float _ppLerpSpeed;

		public readonly AbilityCooldown Cooldown = new AbilityCooldown();

		public Action OnToggled;
	}
}
