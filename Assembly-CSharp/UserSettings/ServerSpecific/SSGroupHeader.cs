namespace UserSettings.ServerSpecific;

public class SSGroupHeader : ServerSpecificSettingBase
{
	public override UserResponseMode ResponseMode => UserResponseMode.None;

	public bool ReducedPadding { get; private set; }

	public override string DebugValue => "N/A";

	public SSGroupHeader(string label, bool reducedPadding = false, string hint = null)
	{
		base.Label = label;
		base.HintDescription = hint;
		ReducedPadding = reducedPadding;
	}

	public override void ApplyDefaultValues()
	{
	}
}
