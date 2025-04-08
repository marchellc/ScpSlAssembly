using System;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173Hud : ScpHudBase
	{
		internal override void Init(ReferenceHub hub)
		{
			base.Init(hub);
			Scp173Role scp173Role = hub.roleManager.CurrentRole as Scp173Role;
			if (scp173Role == null)
			{
				return;
			}
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173BlinkTimer>(out this._blinkAbility);
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173TantrumAbility>(out this._tantrumAbility);
			scp173Role.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeedsAbility);
		}

		protected override void Update()
		{
			base.Update();
			bool isObserved = this._observersTracker.IsObserved;
			float remainingSustainPercent = this._blinkAbility.RemainingSustainPercent;
			float remainingBlinkCooldown = this._blinkAbility.RemainingBlinkCooldown;
			bool flag = isObserved || remainingSustainPercent > 0f;
			this._hudAnimator.SetBool(Scp173Hud.AnimatorHudShownHash, flag);
			this._hudAnimator.SetBool(Scp173Hud.AnimatorHudReadyHash, remainingBlinkCooldown <= 0f);
			this._eyeIndicator.fillAmount = remainingSustainPercent;
			this._timer.text = (flag ? string.Format("{0:F1}s", remainingBlinkCooldown) : string.Empty);
			this._timer.color = Color.Lerp(Color.clear, Color.white, remainingSustainPercent);
			this.UpdateCooldown(this._tantrumCooldown, this._tantrumAbility.Cooldown);
			this.UpdateCooldown(this._breakneckSpeedsCooldown, this._breakneckSpeedsAbility.Cooldown);
			this._eyeIndicator.sprite = ((remainingBlinkCooldown <= 0f) ? this._bloodshotEye : this._openEye);
		}

		private void UpdateCooldown(Image target, AbilityCooldown cooldown)
		{
			GameObject gameObject = target.transform.parent.gameObject;
			if (cooldown.IsReady)
			{
				gameObject.SetActive(false);
				return;
			}
			float readiness = cooldown.Readiness;
			float num = Mathf.Lerp(0f, 0f, readiness);
			target.transform.Rotate(num * Time.deltaTime * Vector3.back);
			target.fillAmount = readiness;
			gameObject.SetActive(true);
		}

		[SerializeField]
		private Animator _hudAnimator;

		[SerializeField]
		private Image _eyeIndicator;

		[SerializeField]
		private Image _tantrumCooldown;

		[SerializeField]
		private Image _breakneckSpeedsCooldown;

		[SerializeField]
		private TextMeshProUGUI _timer;

		[SerializeField]
		private Sprite _bloodshotEye;

		[SerializeField]
		private Sprite _openEye;

		private Scp173ObserversTracker _observersTracker;

		private Scp173BlinkTimer _blinkAbility;

		private Scp173TantrumAbility _tantrumAbility;

		private Scp173BreakneckSpeedsAbility _breakneckSpeedsAbility;

		private const float RotateSpeedFirst = 0f;

		private const float RotateSpeedLast = 0f;

		private static readonly int AnimatorHudShownHash = Animator.StringToHash("Shown");

		private static readonly int AnimatorHudReadyHash = Animator.StringToHash("Ready");
	}
}
