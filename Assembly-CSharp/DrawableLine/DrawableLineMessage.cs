using Mirror;
using UnityEngine;

namespace DrawableLine;

public readonly struct DrawableLineMessage : NetworkMessage
{
	private readonly float? _duration;

	private readonly Color? _color;

	private readonly Vector3[] _positions;

	public DrawableLineMessage(float? duration, Color? color, Vector3[] positions)
	{
		this._duration = duration;
		this._color = color;
		this._positions = positions;
	}

	public DrawableLineMessage(NetworkReader reader)
	{
		this._duration = reader.ReadFloatNullable();
		this._color = reader.ReadColorNullable();
		this._positions = reader.ReadArray<Vector3>();
	}

	public void WriteMessage(NetworkWriter writer)
	{
		writer.WriteFloatNullable(this._duration);
		writer.WriteColorNullable(this._color);
		writer.WriteArray(this._positions);
	}

	public void GenerateLine()
	{
		DrawableLines.ClientGenerateLine(this._duration, this._color, this._positions);
	}
}
