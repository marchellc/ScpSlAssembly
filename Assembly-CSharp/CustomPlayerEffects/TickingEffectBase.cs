using UnityEngine;

namespace CustomPlayerEffects;

public abstract class TickingEffectBase : StatusEffectBase
{
	[Tooltip("Used to track intervals/timers/etc without every effect needing to redefine a unique float.")]
	public float TimeBetweenTicks = 1f;

	private float _timeTillTick;

	protected abstract void OnTick();

	protected override void Enabled()
	{
		base.Enabled();
		this._timeTillTick = this.TimeBetweenTicks;
	}

	protected override void OnEffectUpdate()
	{
		base.OnEffectUpdate();
		this._timeTillTick -= Time.deltaTime;
		if (!(this._timeTillTick > 0f))
		{
			this._timeTillTick += this.TimeBetweenTicks;
			this.OnTick();
		}
	}
}
