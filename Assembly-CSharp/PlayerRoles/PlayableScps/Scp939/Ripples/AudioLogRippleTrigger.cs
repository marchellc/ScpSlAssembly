using System.Collections.Generic;
using MapGeneration.Spawnables;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class AudioLogRippleTrigger : RippleTriggerBase
{
	private readonly Dictionary<AudioLog, float> _cooldowns = new Dictionary<AudioLog, float>();

	public float CooldownPerLog = 1.5f;

	public float RangeMultiplier = 1.35f;

	public override void ResetObject()
	{
		base.ResetObject();
		this._cooldowns.Clear();
	}

	private void Update()
	{
		if (!base.IsLocalOrSpectated)
		{
			return;
		}
		foreach (AudioLog instance in AudioLog.Instances)
		{
			if (this.CanTriggerRipple(instance))
			{
				float maxRange = instance.MaxHearingRange * this.RangeMultiplier;
				base.PlayInRange(instance.PlayingLocation, maxRange, Color.red);
				this._cooldowns[instance] = Time.time + this.CooldownPerLog;
			}
		}
	}

	private bool CanTriggerRipple(AudioLog audioLog)
	{
		if (!audioLog.IsPlaying)
		{
			return false;
		}
		if (!this._cooldowns.TryGetValue(audioLog, out var value))
		{
			return true;
		}
		return value <= Time.time;
	}
}
