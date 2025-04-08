using System;
using System.Collections.Generic;
using System.Diagnostics;

public class RateLimiter
{
	public bool InstantReady
	{
		get
		{
			return this._instantCooldown == 0f || !this._instInput.IsRunning || this._instInput.Elapsed.TotalSeconds >= (double)this._instantCooldown;
		}
	}

	public bool RateReady
	{
		get
		{
			if (!this._useWindowLimiter)
			{
				return true;
			}
			int num = this._inputs.Count;
			if (num < this._windowLength)
			{
				return true;
			}
			while (num > 0 && this._inputs.Peek().Elapsed.TotalSeconds >= (double)this._windowCooldown)
			{
				num--;
				this._inputs.Dequeue();
			}
			return num < this._windowLength;
		}
	}

	public bool AllReady
	{
		get
		{
			return this.InstantReady && this.RateReady;
		}
	}

	public RateLimiter(float instantCooldown, int maxInputs, float timeWindow)
	{
		this._instantCooldown = instantCooldown;
		this._windowCooldown = timeWindow;
		this._windowLength = maxInputs;
		this._inputs = new Queue<Stopwatch>();
		this._instInput = new Stopwatch();
		this._useWindowLimiter = true;
	}

	public RateLimiter(float instantCooldown)
	{
		this._instantCooldown = instantCooldown;
		this._instInput = new Stopwatch();
		this._useWindowLimiter = false;
	}

	public void RegisterInput()
	{
		this._instInput.Restart();
		if (this._useWindowLimiter)
		{
			this._inputs.Enqueue(Stopwatch.StartNew());
		}
	}

	private readonly float _instantCooldown;

	private readonly float _windowCooldown;

	private readonly Queue<Stopwatch> _inputs;

	private readonly Stopwatch _instInput;

	private readonly int _windowLength;

	private readonly bool _useWindowLimiter;
}
