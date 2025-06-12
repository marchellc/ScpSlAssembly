using UnityEngine;

namespace CustomPlayerEffects;

public class AmnesiaVision : StatusEffectBase, ISoundtrackMutingEffect
{
	private float _lastTime;

	public bool MuteSoundtrack => false;

	public float LastActive => Time.timeSinceLevelLoad - this._lastTime;

	protected override void Enabled()
	{
		base.Enabled();
		this._lastTime = Time.timeSinceLevelLoad;
	}
}
