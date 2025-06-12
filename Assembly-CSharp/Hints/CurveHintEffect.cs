using System;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace Hints;

public abstract class CurveHintEffect : HintEffect
{
	private AnimationCurve _curve;

	protected AnimationCurve Curve
	{
		get
		{
			return this._curve;
		}
		set
		{
			if (value != null && value.length > 0)
			{
				this.IterationScalar = value[value.length - 1].time - value[0].time;
				this.IterationScalar *= this.IterationScalar;
			}
			else
			{
				this.IterationScalar = 0f;
			}
			this._curve = value;
		}
	}

	protected float IterationScalar { get; private set; }

	protected CurveHintEffect(float startScalar = 0f, float durationScalar = 1f)
		: base(startScalar, durationScalar)
	{
	}

	protected CurveHintEffect(AnimationCurve curve, float startScalar = 0f, float durationScalar = 1f)
		: this(startScalar, durationScalar)
	{
		this.Curve = curve ?? throw new ArgumentNullException("curve");
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		this.Curve = reader.ReadAnimationCurve();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteAnimationCurve(this.Curve);
	}
}
