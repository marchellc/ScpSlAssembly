using UnityEngine;

public class Remap
{
	private readonly float _inMin;

	private readonly float _inMax;

	private readonly float _outMin;

	private readonly float _outMax;

	private readonly bool _clamped;

	public float Get(float f)
	{
		return Remap.Evaluate(this._inMin, this._inMax, this._outMin, this._outMax, f, this._clamped);
	}

	public Remap(float inMin, float inMax, float outMin, float outMax, bool clamped = true)
	{
		this._inMin = inMin;
		this._inMax = inMax;
		this._outMin = outMin;
		this._outMax = outMax;
		this._clamped = clamped;
	}

	public static float Evaluate(float inMin, float inMax, float outMin, float outMax, float inValue, bool clamped = true)
	{
		float num = ((inMin != inMax) ? ((inValue - inMin) / (inMax - inMin)) : 0f);
		float num2 = outMin + (outMax - outMin) * num;
		if (!clamped)
		{
			return num2;
		}
		float min;
		float max;
		if (outMin < outMax)
		{
			min = outMin;
			max = outMax;
		}
		else
		{
			min = outMax;
			max = outMin;
		}
		return Mathf.Clamp(num2, min, max);
	}
}
