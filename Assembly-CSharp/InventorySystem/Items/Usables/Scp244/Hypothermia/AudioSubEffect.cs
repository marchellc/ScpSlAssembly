using System;
using System.Diagnostics;
using AudioPooling;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia
{
	public class AudioSubEffect : HypothermiaSubEffectBase, ISoundtrackMutingEffect
	{
		public bool MuteSoundtrack { get; private set; }

		public override bool IsActive
		{
			get
			{
				return false;
			}
		}

		private void UpdateShake(float curTemp)
		{
			if (!base.Hub.IsHuman())
			{
				curTemp = 0f;
			}
			float num = this._shakingOverTemperature.Evaluate(curTemp);
			float num2 = (base.IsLocalPlayer ? 1f : this._thirdpersonShakeVolume);
			this._shakingSoundSource.volume = num * num2;
		}

		private void UpdateExposure(bool isExposed)
		{
			if (!base.IsLocalPlayer || !base.Hub.IsAlive())
			{
				isExposed = false;
			}
			this.MuteSoundtrack = isExposed;
			this._fogSoundtrack.volume = Mathf.Lerp(this._fogSoundtrack.volume, (float)(isExposed ? 1 : 0), Time.deltaTime * this._soundtrackFadeSpeed);
			if (isExposed == this._prevExposed)
			{
				return;
			}
			this._prevExposed = isExposed;
			if (isExposed)
			{
				if (this._enterSfxCooldown.Elapsed.TotalSeconds > 1.5)
				{
					AudioSourcePoolManager.Play2D(this._enterFogSound, 1f, MixerChannel.DefaultSfx, 1f);
					return;
				}
			}
			else
			{
				this._enterSfxCooldown.Restart();
			}
		}

		internal override void UpdateEffect(float curExposure)
		{
			this.UpdateExposure(curExposure > 0f);
			this.UpdateShake(this._temperature.CurTemperature);
		}

		[SerializeField]
		private TemperatureSubEffect _temperature;

		[SerializeField]
		private AudioSource _fogSoundtrack;

		[SerializeField]
		private float _soundtrackFadeSpeed;

		[SerializeField]
		private AudioClip _enterFogSound;

		[SerializeField]
		private AnimationCurve _shakingOverTemperature;

		[SerializeField]
		private AudioSource _shakingSoundSource;

		[SerializeField]
		private float _thirdpersonShakeVolume;

		private bool _prevExposed;

		private readonly Stopwatch _enterSfxCooldown = Stopwatch.StartNew();

		private const float SfxCooldown = 1.5f;
	}
}
