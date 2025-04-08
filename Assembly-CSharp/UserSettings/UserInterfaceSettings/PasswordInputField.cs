using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.UserInterfaceSettings
{
	public class PasswordInputField : MonoBehaviour
	{
		private void Awake()
		{
			this._inputField = base.GetComponent<TMP_InputField>();
			this._prevValue = !this._toggleVisibility.isOn;
		}

		private void Update()
		{
			bool isOn = this._toggleVisibility.isOn;
			if (this._prevValue == isOn)
			{
				return;
			}
			this._inputField.contentType = (isOn ? this._trueType : this._falseType);
			this._inputField.ForceLabelUpdate();
			this._prevValue = isOn;
		}

		private TMP_InputField _inputField;

		[SerializeField]
		private Toggle _toggleVisibility;

		[SerializeField]
		private TMP_InputField.ContentType _falseType;

		[SerializeField]
		private TMP_InputField.ContentType _trueType;

		private bool _prevValue;
	}
}
