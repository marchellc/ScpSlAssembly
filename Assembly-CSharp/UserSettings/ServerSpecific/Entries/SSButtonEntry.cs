using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSButtonEntry : HoldableButton, ISSEntry
	{
		public override float HoldTime
		{
			get
			{
				return this._holdTime;
			}
		}

		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSButton;
		}

		public void Init(ServerSpecificSettingBase settingBase)
		{
			this._setting = settingBase as SSButton;
			this._label.Set(this._setting);
			this._holdTime = this._setting.HoldTimeSeconds;
			if (this._holdTime > 0f)
			{
				base.OnHeld.AddListener(new UnityAction(this.OnTrigger));
			}
			else
			{
				base.onClick.AddListener(new UnityAction(this.OnTrigger));
			}
			this._buttonText.text = this._setting.ButtonText;
		}

		private void OnTrigger()
		{
			this._setting.ClientSendValue();
		}

		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private TMP_Text _buttonText;

		private float _holdTime;

		private SSButton _setting;
	}
}
