using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079KeyAbilityGui : Scp079GuiElementBase
	{
		internal void Setup(bool isReady, string description, ActionName key, bool createSpace)
		{
			float num = (createSpace ? this._rescaleParams.x : this._rescaleParams.y);
			this._rescaleTransform.sizeDelta = new Vector2(this._rescaleParams.z, num);
			base.gameObject.SetActive(true);
			this.SetupKeycode(NewInput.GetKey(key, KeyCode.None));
			Color color = (isReady ? this._readyColor : this._unavailableColor);
			this._description.text = description;
			this._description.color = color;
			this._background.color = color;
		}

		private void SetupKeycode(KeyCode keycode)
		{
			if (keycode == this._prevKeycode)
			{
				return;
			}
			bool flag = keycode == KeyCode.Mouse0;
			bool flag2 = keycode == KeyCode.Mouse1;
			this._prevKeycode = keycode;
			this._lmbObj.SetActive(flag);
			this._rmbObj.SetActive(flag2);
			bool flag3 = flag || flag2;
			this._keyText.gameObject.SetActive(!flag3);
			if (!flag3)
			{
				this._keyText.text = new ReadableKeyCode(keycode).ShortVersion;
			}
		}

		[SerializeField]
		private Image _background;

		[SerializeField]
		private Color _unavailableColor;

		[SerializeField]
		private Color _readyColor;

		[SerializeField]
		private TextMeshProUGUI _description;

		[SerializeField]
		private TextMeshProUGUI _keyText;

		[SerializeField]
		private GameObject _lmbObj;

		[SerializeField]
		private GameObject _rmbObj;

		[SerializeField]
		private RectTransform _rescaleTransform;

		[SerializeField]
		private Vector3 _rescaleParams;

		private KeyCode _prevKeycode;
	}
}
