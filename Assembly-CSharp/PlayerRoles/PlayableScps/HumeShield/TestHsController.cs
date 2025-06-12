using UnityEngine;

namespace PlayerRoles.PlayableScps.HumeShield;

public class TestHsController : HumeShieldModuleBase
{
	[SerializeField]
	private float _regeneration;

	[SerializeField]
	private float _maxAmount;

	[Space]
	[SerializeField]
	private Color _color;

	[SerializeField]
	private bool _colorActive;

	[Space]
	[SerializeField]
	private float _amountToModify;

	[SerializeField]
	private bool _apply;

	[SerializeField]
	private bool _hideWhenEmpty;

	public override float HsMax => this._maxAmount;

	public override float HsRegeneration => this._regeneration;

	public override Color? HsWarningColor
	{
		get
		{
			if (!this._colorActive)
			{
				return null;
			}
			return this._color;
		}
	}

	public override bool HideWhenEmpty => this._hideWhenEmpty;

	private void Update()
	{
		if (this._apply)
		{
			base.HsCurrent += this._amountToModify;
			this._apply = false;
		}
	}
}
