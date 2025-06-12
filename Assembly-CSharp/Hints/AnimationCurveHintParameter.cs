using System.Globalization;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace Hints;

public class AnimationCurveHintParameter : HintParameter
{
	private double _offset;

	private string _format;

	private bool _integral;

	private AnimationCurve _curve;

	public static AnimationCurveHintParameter FromNetwork(NetworkReader reader)
	{
		AnimationCurveHintParameter animationCurveHintParameter = new AnimationCurveHintParameter();
		animationCurveHintParameter.Deserialize(reader);
		return animationCurveHintParameter;
	}

	public AnimationCurveHintParameter(double offset, AnimationCurve curve, string format, bool integral)
	{
		this._offset = offset;
		this._curve = curve;
		this._format = format;
		this._integral = integral;
	}

	protected AnimationCurveHintParameter()
	{
	}

	public override void Serialize(NetworkWriter writer)
	{
		writer.WriteDouble(this._offset);
		writer.WriteString(this._format);
		writer.WriteBool(this._integral);
		writer.WriteAnimationCurve(this._curve);
	}

	public override void Deserialize(NetworkReader reader)
	{
		this._offset = reader.ReadDouble();
		this._format = reader.ReadString();
		this._integral = reader.ReadBool();
		this._curve = reader.ReadAnimationCurve();
	}

	protected override string UpdateState(float progress)
	{
		double num = NetworkTime.time - this._offset;
		float f = this._curve.Evaluate((float)num);
		if (this._integral)
		{
			return Mathf.RoundToInt(f).ToString(this._format, CultureInfo.InvariantCulture);
		}
		return f.ToString(this._format, CultureInfo.InvariantCulture);
	}
}
