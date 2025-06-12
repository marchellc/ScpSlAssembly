using System.Collections.Generic;
using Mirror;
using Utils.Networking;

namespace Hints;

public abstract class Hint : DisplayableObject<SharedHintData>
{
	private HintEffect[] _effects;

	protected HintParameter[] Parameters { get; private set; }

	protected Hint(HintParameter[] parameters, HintEffect[] effects, float durationScalar = 1f)
		: base(durationScalar)
	{
		this._effects = effects;
		this.Parameters = parameters;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		this._effects = reader.ReadHintEffectArray();
		this.Parameters = reader.ReadHintParameterArray();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteHintEffectArray((IReadOnlyCollection<HintEffect>)(object)this._effects);
		writer.WriteHintParameterArray((IReadOnlyCollection<HintParameter>)(object)this.Parameters);
	}
}
