namespace PlayerStatsSystem;

public abstract class StatBase
{
	public abstract float CurValue { get; set; }

	public abstract float MinValue { get; }

	public abstract float MaxValue { get; set; }

	public float NormalizedValue
	{
		get
		{
			if (this.MinValue != this.MaxValue)
			{
				return (this.CurValue - this.MinValue) / (this.MaxValue - this.MinValue);
			}
			return 0f;
		}
	}

	public ReferenceHub Hub { get; private set; }

	internal virtual void Init(ReferenceHub ply)
	{
		this.Hub = ply;
	}

	internal virtual void Update()
	{
	}

	internal virtual void ClassChanged()
	{
	}
}
