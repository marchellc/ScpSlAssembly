using System;
using System.Diagnostics;

public class SustainedValue
{
	public float Value
	{
		get
		{
			return this._peak;
		}
		set
		{
			float num = this.ConditionalAbs(value);
			float num2 = this.ConditionalAbs(this._peak);
			bool flag = this._susTime > 0f;
			if ((this._inverse && num <= num2) || (!this._inverse && num >= num2))
			{
				this._peak = value;
				if (flag)
				{
					this._lastUpdateSw.Restart();
					return;
				}
			}
			else if (flag && this._lastUpdateSw.Elapsed.TotalSeconds >= (double)this._susTime)
			{
				this._peak = value;
			}
		}
	}

	public SustainedValue(float initialValue, float sustainTime, bool inverse, bool absolute)
	{
		this._peak = initialValue;
		this._susTime = sustainTime;
		this._inverse = inverse;
		this._abs = absolute;
		if (sustainTime > 0f)
		{
			this._lastUpdateSw = Stopwatch.StartNew();
		}
	}

	private float ConditionalAbs(float val)
	{
		if (!this._abs || val >= 0f)
		{
			return val;
		}
		return -val;
	}

	private float _peak;

	private readonly float _susTime;

	private readonly bool _inverse;

	private readonly bool _abs;

	private readonly Stopwatch _lastUpdateSw;
}
