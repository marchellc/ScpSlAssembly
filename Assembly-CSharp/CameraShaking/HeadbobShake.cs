using System;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;

namespace CameraShaking
{
	public class HeadbobShake : IShakeEffect
	{
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
			Vector3 vector = ((!HeadbobShake.EnableBobbing.Value) ? Vector3.zero : ply.transform.TransformDirection(this._model.HeadBobPosition));
			Vector3? vector2 = new Vector3?(vector);
			shakeValues = new ShakeEffectValues(null, null, vector2, 1f, 0f, 0f);
			return true;
		}

		private readonly AnimatedCharacterModel _model;

		private static HeadbobShake _mostCurrent;

		private static readonly CachedUserSetting<bool> EnableBobbing = new CachedUserSetting<bool>(MiscVideoSetting.HeadBobbing);
	}
}
