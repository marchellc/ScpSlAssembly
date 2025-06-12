using Mirror;

namespace Hints;

public abstract class IdHintParameter : HintParameter
{
	private bool _stopFormatting;

	protected int Id { get; private set; }

	protected IdHintParameter()
	{
	}

	protected IdHintParameter(byte id)
	{
		this.Id = id;
	}

	public override void Deserialize(NetworkReader reader)
	{
		this.Id = reader.ReadInt();
	}

	public override void Serialize(NetworkWriter writer)
	{
		writer.WriteInt(this.Id);
	}

	protected override string UpdateState(float progress)
	{
		if (this._stopFormatting)
		{
			return null;
		}
		return this.FormatId(progress, out this._stopFormatting);
	}

	protected abstract string FormatId(float progress, out bool stopFormatting);
}
