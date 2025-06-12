using Elevators;
using Mirror;
using UnityEngine;

namespace Hazards;

[RequireComponent(typeof(TransformElevatorFollower))]
public abstract class TemporaryHazard : EnvironmentalHazard
{
	public float Elapsed;

	private bool _active;

	private bool _destroyed;

	public override bool IsActive
	{
		get
		{
			if (!this._destroyed)
			{
				return this._active;
			}
			return false;
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
		for (int num = base.AffectedPlayers.Count - 1; num >= 0; num--)
		{
			this.OnExit(base.AffectedPlayers[num]);
		}
	}

	protected override void Start()
	{
		base.Start();
		this.IsActive = true;
	}

	protected override void Update()
	{
		base.Update();
		if (NetworkServer.active && this.IsActive)
		{
			if (this.Elapsed > this.HazardDuration)
			{
				this.ServerDestroy();
			}
			else
			{
				this.Elapsed += this.DecaySpeed * Time.deltaTime;
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}
