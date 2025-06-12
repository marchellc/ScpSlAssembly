using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;

namespace CameraShaking;

public class HeadbobShake : IShakeEffect
{
	private readonly AnimatedCharacterModel _model;

	private static HeadbobShake _mostCurrent;

	private static readonly CachedUserSetting<bool> EnableBobbing = new CachedUserSetting<bool>(MiscVideoSetting.HeadBobbing);

	public HeadbobShake(AnimatedCharacterModel model)
	{
		this._model = model;
		HeadbobShake._mostCurrent = this;
	}

	public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
	{
		if (this._model.Pooled || !this._model.IsTracked || this != HeadbobShake._mostCurrent)
		{
			shakeValues = ShakeEffectValues.None;
			return false;
		}
		Vector3 value = ((!HeadbobShake.EnableBobbing.Value) ? Vector3.zero : ply.transform.TransformDirection(this._model.HeadBobPosition));
		Vector3? rootCameraPositionOffset = value;
		shakeValues = new ShakeEffectValues(null, null, rootCameraPositionOffset);
		return true;
	}
}
