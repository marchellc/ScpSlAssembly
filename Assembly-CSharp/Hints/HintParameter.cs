namespace Hints;

public abstract class HintParameter : NetworkObject<SharedHintData>
{
	public string Formatted { get; private set; }

	protected abstract string UpdateState(float progress);

	public bool Update(float progress)
	{
		string text = this.UpdateState(progress);
		if (text != null)
		{
			this.Formatted = text;
			return true;
		}
		return false;
	}
}
