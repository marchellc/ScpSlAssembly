using Mirror;
using UnityEngine;

namespace PlayerRoles.Subroutines;

public class AbilityCooldown : IAbilityCooldown
{
	public double InitialTime { get; set; }

	public double NextUse { get; set; }

	public virtual bool IsReady => NetworkTime.time >= this.NextUse;

	public float Remaining
	{
		get
		{
			return Mathf.Max(0f, (float)(this.NextUse - NetworkTime.time));
		}
		set
		{
			this.NextUse = NetworkTime.time + (double)value;
		}
	}

	public float Readiness => Mathf.Clamp01((float)((NetworkTime.time - this.InitialTime) / (this.NextUse - this.InitialTime)));

	public virtual void WriteCooldown(NetworkWriter writer)
	{
		writer.WriteDouble(this.NextUse);
	}

	public virtual void ReadCooldown(NetworkReader reader)
	{
		this.InitialTime = NetworkTime.time;
		this.NextUse = reader.ReadDouble();
	}

	public virtual void Clear()
	{
		this.InitialTime = 0.0;
		this.NextUse = 1.0;
	}

	public virtual void Trigger(double cooldown)
	{
		this.InitialTime = NetworkTime.time;
		this.NextUse = this.InitialTime + cooldown;
	}
}
