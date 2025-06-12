using System;

[Serializable]
public struct AlphaWarheadSyncInfo : IEquatable<AlphaWarheadSyncInfo>
{
	public byte ScenarioId;

	public WarheadScenarioType ScenarioType;

	public double StartTime;

	public bool InProgress => this.StartTime != 0.0;

	public override int GetHashCode()
	{
		return (int)(this.StartTime * 10.0) % 32767 * 255 + this.ScenarioId + (int)this.ScenarioType;
	}

	public static bool operator ==(AlphaWarheadSyncInfo left, AlphaWarheadSyncInfo right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(AlphaWarheadSyncInfo left, AlphaWarheadSyncInfo right)
	{
		return !left.Equals(right);
	}

	public bool Equals(AlphaWarheadSyncInfo other)
	{
		if (other.ScenarioId == this.ScenarioId && other.StartTime == this.StartTime)
		{
			return other.ScenarioType == this.ScenarioType;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is AlphaWarheadSyncInfo other)
		{
			return this.Equals(other);
		}
		return false;
	}
}
