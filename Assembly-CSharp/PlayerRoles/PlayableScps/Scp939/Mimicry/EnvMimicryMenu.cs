using System;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class EnvMimicryMenu : MimicryMenuBase
	{
		protected override void Setup(Scp939Role role)
		{
			base.Setup(role);
			role.SubroutineModule.TryGetSubroutine<EnvironmentalMimicry>(out this._envMimicry);
		}

		private void UpdateFade(bool instant)
		{
			bool isReady = this._envMimicry.Cooldown.IsReady;
			float num = (instant ? 1f : (Time.deltaTime * this._fadeSpeed));
			float num2 = (isReady ? 1f : this._fadedAlpha);
			this._fader.alpha = Mathf.MoveTowards(this._fader.alpha, num2, num);
			this._fader.blocksRaycasts = isReady;
			float num3 = (float)(isReady ? 0 : 1);
			this._cooldownText.alpha = Mathf.MoveTowards(this._cooldownText.alpha, num3, num);
			if (isReady)
			{
				return;
			}
			this._cooldownText.text = this._envMimicry.CooldownText;
		}

		private void OnEnable()
		{
			this.UpdateFade(true);
		}

		private void Update()
		{
			this.UpdateFade(false);
		}

		[SerializeField]
		private CanvasGroup _fader;

		[SerializeField]
		private float _fadedAlpha;

		[SerializeField]
		private float _fadeSpeed;

		[SerializeField]
		private TMP_Text _cooldownText;

		private EnvironmentalMimicry _envMimicry;
	}
}
