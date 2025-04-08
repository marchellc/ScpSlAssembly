using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UserSettings.GUIElements
{
	public class UserSettingsTwoButtons : UserSettingsUIBase<Toggle, bool>, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		protected override UnityEvent<bool> OnValueChangedEvent
		{
			get
			{
				return base.TargetUI.onValueChanged;
			}
		}

		private void Update()
		{
			this.UpdateColors(false);
		}

		private void OnEnable()
		{
			this.UpdateColors(true);
		}

		private void OnDisable()
		{
			this._isHighlighted = false;
		}

		protected void UpdateColors(bool instant)
		{
			if (this._transitionTime <= 0f || instant)
			{
				this._curAnim = (float)(base.TargetUI.isOn ? 1 : 0);
			}
			else
			{
				float num = Time.deltaTime / this._transitionTime;
				if (base.TargetUI.isOn)
				{
					this._curAnim += num;
				}
				else
				{
					this._curAnim -= num;
				}
				this._curAnim = Mathf.Clamp01(this._curAnim);
			}
			RoleAccentColor roleAccentColor = (this._isHighlighted ? this._highlightColor : this._inactiveColor);
			this._trueImage.color = Color.Lerp(roleAccentColor.Color, this._activeColor.Color, this._curAnim);
			this._falseImage.color = Color.Lerp(this._activeColor.Color, roleAccentColor.Color, this._curAnim);
		}

		protected override void SetValueAndTriggerEvent(bool val)
		{
			base.TargetUI.isOn = val;
		}

		protected override void SetValueWithoutNotify(bool val)
		{
			base.TargetUI.SetIsOnWithoutNotify(val);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			this._isHighlighted = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			this._isHighlighted = false;
		}

		[SerializeField]
		private Image _trueImage;

		[SerializeField]
		private Image _falseImage;

		[SerializeField]
		private RoleAccentColor _inactiveColor;

		[SerializeField]
		private RoleAccentColor _highlightColor;

		[SerializeField]
		private RoleAccentColor _activeColor;

		[SerializeField]
		private float _transitionTime;

		private float _curAnim;

		private bool _isHighlighted;
	}
}
