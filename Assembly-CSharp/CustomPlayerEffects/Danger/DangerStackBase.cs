using System.Diagnostics;

namespace CustomPlayerEffects.Danger;

public abstract class DangerStackBase
{
	public readonly Stopwatch TimeTracker = new Stopwatch();

	private bool _isActive;

	public abstract float DangerValue { get; set; }

	public virtual float Duration { get; } = 20f;

	public virtual bool IsActive
	{
		get
		{
			bool flag = this.TimeTracker.IsRunning && this.TimeTracker.Elapsed.TotalSeconds <= (double)this.Duration;
			if (!this._isActive && !flag)
			{
				this.TimeTracker.Stop();
			}
			return this._isActive || flag;
		}
		protected set
		{
			if (value != this._isActive)
			{
				this._isActive = value;
				if (!this._isActive)
				{
					this.TimeTracker.Restart();
				}
			}
		}
	}

	protected ReferenceHub Owner { get; set; }

	public virtual void Initialize(ReferenceHub target)
	{
		this.Owner = target;
	}

	public virtual void Dispose()
	{
	}
}
