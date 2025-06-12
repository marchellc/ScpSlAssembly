using Mirror;

namespace Hints;

public abstract class HintEffect : DisplayableObject<SharedHintData>
{
	public float StartScalar { get; private set; }

	protected HintEffect(float startScalar = 0f, float durationScalar = 1f)
		: base(durationScalar)
	{
		this.StartScalar = startScalar;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		this.StartScalar = reader.ReadFloat();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteFloat(this.StartScalar);
	}
}
