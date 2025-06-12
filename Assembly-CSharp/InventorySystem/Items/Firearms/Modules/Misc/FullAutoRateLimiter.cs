using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class FullAutoRateLimiter
{
	private bool _lastWasReady;

	private float _remainingCooldown;

	public bool Ready => this._remainingCooldown <= 0f;

	public void Update()
	{
		if (this.Ready)
		{
			this._lastWasReady = true;
			return;
		}
		this._lastWasReady = false;
		this._remainingCooldown -= Time.deltaTime;
	}

	public void Clear()
	{
		this._remainingCooldown = 0f;
		this._lastWasReady = true;
	}

	public void Trigger(float cooldown)
	{
		if (this._lastWasReady)
		{
			this._remainingCooldown = cooldown;
		}
		else
		{
			this._remainingCooldown += cooldown;
		}
	}
}
