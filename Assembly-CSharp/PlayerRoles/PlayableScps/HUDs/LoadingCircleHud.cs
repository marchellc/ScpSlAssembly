using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.HUDs
{
	[Serializable]
	public class LoadingCircleHud
	{
		public void Apply(float percent, bool forceHide = false)
		{
			percent = Mathf.Clamp01(percent);
			bool flag = forceHide || (percent == 1f && this._hideWhenFull) || (percent == 0f && this._hideWhenEmpty);
			this._parent.SetActive(!flag);
			if (flag)
			{
				return;
			}
			this._loadingBar.fillAmount = (this._inverseFill ? (1f - percent) : percent);
			this._loadingBar.color = this._colorGradient.Evaluate(percent);
		}

		[SerializeField]
		private GameObject _parent;

		[SerializeField]
		private Image _loadingBar;

		[SerializeField]
		private Gradient _colorGradient;

		[SerializeField]
		private bool _inverseFill;

		[SerializeField]
		private bool _hideWhenFull;

		[SerializeField]
		private bool _hideWhenEmpty;
	}
}
