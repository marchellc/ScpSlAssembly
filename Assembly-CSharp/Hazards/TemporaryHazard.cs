using System;
using Elevators;
using Mirror;
using UnityEngine;

namespace Hazards
{
	[RequireComponent(typeof(TransformElevatorFollower))]
	public abstract class TemporaryHazard : EnvironmentalHazard
	{
		public override bool IsActive
		{
			get
			{
				return !this._destroyed && this._active;
			}
			set
			{
				this._active = value;
			}
		}

		public abstract float HazardDuration { get; set; }

		public virtual float DecaySpeed { get; set; } = 1f;

		[Server]
		public virtual void ServerDestroy()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Hazards.TemporaryHazard::ServerDestroy()' called when server was not active");
				return;
			}
			this._destroyed = true;
			this.IsActive = false;
			base.AffectedPlayers.ForEach(delegate(ReferenceHub n)
			{
				this.OnExit(n);
			});
		}

		protected override void Start()
		{
			base.Start();
			this.IsActive = true;
		}

		protected override void Update()
		{
			base.Update();
			if (!NetworkServer.active || !this.IsActive)
			{
				return;
			}
			if (this.Elapsed > this.HazardDuration)
			{
				this.ServerDestroy();
				return;
			}
			this.Elapsed += this.DecaySpeed * Time.deltaTime;
		}

		public override bool Weaved()
		{
			return true;
		}

		public float Elapsed;

		private bool _active;

		private bool _destroyed;
	}
}
