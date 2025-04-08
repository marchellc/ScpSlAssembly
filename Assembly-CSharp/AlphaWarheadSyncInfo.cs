using System;

[Serializable]
public struct AlphaWarheadSyncInfo : IEquatable<AlphaWarheadSyncInfo>
{
	public bool InProgress
	{
		get
		{
			return this.StartTime != 0.0;
		}
	}

	public override int GetHashCode()
	{
		return (int)((byte)((int)(this.StartTime * 10.0) % 32767 * 255 + (int)this.ScenarioId) + this.ScenarioType);
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
		return other.ScenarioId == this.ScenarioId && other.StartTime == this.StartTime && other.ScenarioType == this.ScenarioType;
	}

	public override bool Equals(object obj)
	{
		if (obj is AlphaWarheadSyncInfo)
		{
			AlphaWarheadSyncInfo alphaWarheadSyncInfo = (AlphaWarheadSyncInfo)obj;
			return this.Equals(alphaWarheadSyncInfo);
		}
		return false;
	}

	public byte ScenarioId;

	public WarheadScenarioType ScenarioType;

	public double StartTime;
}
