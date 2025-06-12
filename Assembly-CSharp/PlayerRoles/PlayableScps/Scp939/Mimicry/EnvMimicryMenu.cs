using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class EnvMimicryMenu : MimicryMenuBase
{
	[SerializeField]
	private CanvasGroup _fader;

	[SerializeField]
	private float _fadedAlpha;

	[SerializeField]
	private float _fadeSpeed;

	[SerializeField]
	private TMP_Text _cooldownText;

	private EnvironmentalMimicry _envMimicry;

	protected override void Setup(Scp939Role role)
	{
		base.Setup(role);
		role.SubroutineModule.TryGetSubroutine<EnvironmentalMimicry>(out this._envMimicry);
	}

	private void UpdateFade(bool instant)
	{
		bool isReady = this._envMimicry.Cooldown.IsReady;
		float maxDelta = (instant ? 1f : (Time.deltaTime * this._fadeSpeed));
		float target = (isReady ? 1f : this._fadedAlpha);
		this._fader.alpha = Mathf.MoveTowards(this._fader.alpha, target, maxDelta);
		this._fader.blocksRaycasts = isReady;
		float target2 = ((!isReady) ? 1 : 0);
		this._cooldownText.alpha = Mathf.MoveTowards(this._cooldownText.alpha, target2, maxDelta);
		if (!isReady)
		{
			this._cooldownText.text = this._envMimicry.CooldownText;
		}
	}

	private void OnEnable()
	{
		this.UpdateFade(instant: true);
	}

	private void Update()
	{
		this.UpdateFade(instant: false);
	}
}
