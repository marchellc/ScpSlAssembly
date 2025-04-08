using System;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HumeShield
{
	public class TestHsController : HumeShieldModuleBase
	{
		public override float HsMax
		{
			get
			{
				return this._maxAmount;
			}
		}

		public override float HsRegeneration
		{
			get
			{
				return this._regeneration;
			}
		}

		public override Color? HsWarningColor
		{
			get
			{
				if (!this._colorActive)
				{
					return null;
				}
				return new Color?(this._color);
			}
		}

		public override bool HideWhenEmpty
		{
			get
			{
				return this._hideWhenEmpty;
			}
		}

		private void Update()
		{
			if (!this._apply)
			{
				return;
			}
			base.HsCurrent += this._amountToModify;
			this._apply = false;
		}

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
	}
}
