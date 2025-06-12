namespace CustomPlayerEffects.Danger;

public class WarheadDanger : DangerStackBase
{
	public override float DangerValue { get; set; } = 1f;

	public override void Initialize(ReferenceHub target)
	{
		base.Initialize(target);
		this.UpdateState(AlphaWarheadController.InProgress);
		AlphaWarheadController.OnProgressChanged += UpdateState;
		AlphaWarheadController.OnDetonated += OnDetonated;
	}

	public override void Dispose()
	{
		base.Dispose();
		AlphaWarheadController.OnProgressChanged -= UpdateState;
		AlphaWarheadController.OnDetonated -= OnDetonated;
	}

	private void UpdateState(bool warheadActive)
	{
		this.IsActive = warheadActive && !AlphaWarheadController.Detonated;
	}

	private void OnDetonated()
	{
		this.UpdateState(AlphaWarheadController.InProgress);
	}
}
