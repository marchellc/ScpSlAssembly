using PlayerRoles.PlayableScps.HUDs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096Hud : ViewmodelScpHud
{
	[SerializeField]
	private AbilityHud _rageDuration;

	[SerializeField]
	private AbilityHud _chargeCooldown;

	[SerializeField]
	private Image[] _keyCircles;

	[SerializeField]
	private Image _rageEnterSustainCircle;

	[SerializeField]
	private GameObject _docileCircles;

	[SerializeField]
	private ScpWarningHud _rageInfo;

	[SerializeField]
	private Volume _rageVolume;

	[SerializeField]
	private float _rageVolumeDelta;

	private Scp096Role _scp096;

	private Scp096RageCycleAbility _rageCycle;

	private Scp096RageManager _rageManager;

	private Scp096ChargeAbility _chargeAbility;

	internal override void Init(ReferenceHub hub)
	{
		base.Init(hub);
		this._scp096 = hub.roleManager.CurrentRole as Scp096Role;
		this._scp096.SubroutineModule.TryGetSubroutine<Scp096RageCycleAbility>(out this._rageCycle);
		this._scp096.SubroutineModule.TryGetSubroutine<Scp096RageManager>(out this._rageManager);
		this._scp096.SubroutineModule.TryGetSubroutine<Scp096ChargeAbility>(out this._chargeAbility);
		this._rageDuration.Setup(this._rageManager.HudRageDuration, null);
		this._chargeCooldown.Setup(this._chargeAbility.Cooldown, this._chargeAbility.Duration);
		this._rageInfo.gameObject.SetActive(hub.isLocalPlayer);
		this.UpdateColorGrading(1f);
	}

	protected override void Update()
	{
		base.Update();
		this.UpdateRageInfo();
		this.UpdateColorGrading(this._rageVolumeDelta * Time.deltaTime);
	}

	private void UpdateColorGrading(float maxDelta)
	{
		this._rageVolume.weight = Mathf.MoveTowards(this._rageVolume.weight, this._rageManager.IsEnragedOrDistressed ? 1 : 0, maxDelta);
	}

	private void UpdateRageInfo()
	{
		this._keyCircles.ForEach(delegate(Image x)
		{
			x.fillAmount = this._rageCycle.HudEnterRageKeyProgress;
		});
		switch (this._scp096.StateController.RageState)
		{
		case Scp096RageState.Docile:
		{
			float hudEnterRageSustain = this._rageCycle.HudEnterRageSustain;
			this._docileCircles.SetActive(hudEnterRageSustain > 0f);
			this._rageEnterSustainCircle.fillAmount = hudEnterRageSustain;
			this._rageDuration.Update(forceHidden: true);
			this._chargeCooldown.Update(forceHidden: true);
			if (!(hudEnterRageSustain <= 0f))
			{
				this.SetWarning(Scp096HudTranslation.EnterRageKeyInfo, ActionName.Reload, hudEnterRageSustain);
			}
			break;
		}
		case Scp096RageState.Enraged:
			this._rageDuration.Update();
			this._chargeCooldown.Update();
			this._docileCircles.SetActive(value: false);
			this.SetWarning(Scp096HudTranslation.ExitRageKeyInfo, ActionName.Reload);
			break;
		default:
			this._rageDuration.Update(forceHidden: true);
			this._chargeCooldown.Update(forceHidden: true);
			this._docileCircles.SetActive(value: false);
			this._rageInfo.SetText(string.Empty);
			break;
		}
	}

	private void SetWarning(Scp096HudTranslation key, ActionName action, float duration = 3.8f)
	{
		if (!Translations.TryGet(key, out var tr))
		{
			tr = "Hold {0} to " + key;
		}
		this._rageInfo.SetText(string.Format(tr, $"<color=red>{new ReadableKeyCode(action)}</color>"), duration);
	}
}
