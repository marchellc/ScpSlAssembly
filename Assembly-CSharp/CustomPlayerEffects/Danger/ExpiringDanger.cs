namespace CustomPlayerEffects.Danger;

public class ExpiringDanger : DangerStackBase
{
	public override float DangerValue { get; set; }

	public ExpiringDanger(float dangerValue, ReferenceHub owner)
	{
		this.DangerValue = dangerValue;
		base.Owner = owner;
		this.Initialize(base.Owner);
	}

	public override void Initialize(ReferenceHub target)
	{
		base.TimeTracker.Start();
	}
}
