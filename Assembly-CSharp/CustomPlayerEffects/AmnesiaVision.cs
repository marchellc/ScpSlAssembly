using UnityEngine;

namespace CustomPlayerEffects;

public class AmnesiaVision : StatusEffectBase, ISoundtrackMutingEffect
{
	private float _lastTime;

	public bool MuteSoundtrack => false;

	public float LastActive => Time.timeSinceLevelLoad - _lastTime;

	protected override void Enabled()
	{
		base.Enabled();
		_lastTime = Time.timeSinceLevelLoad;
	}
}
