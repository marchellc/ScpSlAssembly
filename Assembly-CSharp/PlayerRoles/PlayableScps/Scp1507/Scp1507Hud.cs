using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507Hud : ScpHudBase
{
	[SerializeField]
	private AbilityHud _attackElement;

	[SerializeField]
	private AbilityHud _vocalizeElement;

	[SerializeField]
	private GameObject _swarmElement;

	[SerializeField]
	private Image _swarmCircle;

	[SerializeField]
	private TMP_Text _swarmSizeCounter;

	[SerializeField]
	private Color _swarmActiveColor;

	[SerializeField]
	private Color _swarmInactiveColor;

	[SerializeField]
	private float _colorLerpSpeed;

	[SerializeField]
	private TMP_Text _allyCounter;

	private Scp1507SwarmAbility _swarmAbility;

	private GameObject _trackerRoot;

	internal override void Init(ReferenceHub hub)
	{
		base.Init(hub);
		SubroutineManagerModule subroutineModule = (base.Hub.roleManager.CurrentRole as Scp1507Role).SubroutineModule;
		subroutineModule.TryGetSubroutine<Scp1507AttackAbility>(out var subroutine);
		subroutineModule.TryGetSubroutine<Scp1507VocalizeAbility>(out var subroutine2);
		subroutineModule.TryGetSubroutine<Scp1507SwarmAbility>(out this._swarmAbility);
		this._trackerRoot = this._allyCounter.transform.parent.gameObject;
		this._attackElement.Setup(subroutine.Cooldown, null);
		this._vocalizeElement.Setup(subroutine2.Cooldown, null);
	}

	protected override void Update()
	{
		base.Update();
		this._attackElement.Update();
		this._vocalizeElement.Update();
		if (this._swarmAbility.Multiplier <= 0f)
		{
			this._swarmElement.SetActive(value: false);
			return;
		}
		this._swarmElement.SetActive(value: true);
		int flockSize = this._swarmAbility.FlockSize;
		Color b = ((flockSize > 0) ? this._swarmActiveColor : this._swarmInactiveColor);
		this._swarmCircle.fillAmount = this._swarmAbility.Multiplier;
		this._swarmCircle.color = Color.Lerp(this._swarmCircle.color, b, Time.deltaTime * this._colorLerpSpeed);
		this._swarmSizeCounter.text = ((flockSize > 1) ? ("x" + flockSize) : string.Empty);
	}

	protected override void UpdateCounter()
	{
	}
}
