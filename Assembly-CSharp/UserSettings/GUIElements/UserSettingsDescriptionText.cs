using System;
using TMPro;
using UnityEngine;

namespace UserSettings.GUIElements
{
	public class UserSettingsDescriptionText : MonoBehaviour
	{
		private UserSettingsEntryDescription Current
		{
			get
			{
				return UserSettingsEntryDescription.CurrentDescription;
			}
		}

		private void Awake()
		{
			this._text = base.GetComponent<TMP_Text>();
		}

		private void OnEnable()
		{
			this._text.alpha = 0f;
		}

		private void Update()
		{
			if (this._prev == this.Current)
			{
				this.UpdateCurrent();
				return;
			}
			this.DisablePrevious();
		}

		private void DisablePrevious()
		{
			if (this._text.alpha > 0f)
			{
				this._text.alpha -= this._transitionSpeed * Time.deltaTime;
				return;
			}
			this._prev = UserSettingsEntryDescription.CurrentDescription;
		}

		private void UpdateCurrent()
		{
			if (this.Current == null)
			{
				this._text.alpha = 0f;
				return;
			}
			this._text.text = this.Current.Text;
			this._text.alpha = Mathf.MoveTowards(this._text.alpha, 1f, Time.deltaTime * this._transitionSpeed);
		}

		private TMP_Text _text;

		private UserSettingsEntryDescription _prev;

		[SerializeField]
		private float _transitionSpeed;
	}
}
