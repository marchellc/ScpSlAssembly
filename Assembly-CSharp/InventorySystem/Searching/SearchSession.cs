using System;
using Mirror;
using Utils;

namespace InventorySystem.Searching;

public struct SearchSession : ISearchSession, NetworkMessage, IEquatable<SearchSession>
{
	public ISearchable Target { get; set; }

	public double InitialTime { get; set; }

	public double FinishTime { get; set; }

	public double Duration => this.FinishTime - this.InitialTime;

	public double Progress => MoreMath.InverseLerp(this.InitialTime, this.FinishTime, NetworkTime.time);

	public SearchSession(double initialTime, double finishTime, ISearchable target)
	{
		this.Target = target;
		this.InitialTime = initialTime;
		this.FinishTime = finishTime;
	}

	public void Deserialize(NetworkReader reader)
	{
		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		this.Target = ((networkIdentity == null) ? null : networkIdentity.GetComponent<ISearchable>());
		this.InitialTime = reader.ReadDouble();
		this.FinishTime = reader.ReadDouble();
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteNetworkIdentity(this.Target?.netIdentity);
		writer.WriteDouble(this.InitialTime);
		writer.WriteDouble(this.FinishTime);
	}

	public bool Equals(SearchSession other)
	{
		if (object.Equals(this.Target, other.Target) && this.InitialTime.Equals(other.InitialTime))
		{
			return this.FinishTime.Equals(other.FinishTime);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SearchSession other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((this.Target != null) ? this.Target.GetHashCode() : 0) * 397) ^ this.InitialTime.GetHashCode()) * 397) ^ this.FinishTime.GetHashCode();
	}
}
