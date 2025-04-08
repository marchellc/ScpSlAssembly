using System;
using TMPro;
using UnityEngine;

namespace UserSettings.VideoSettings
{
	public class ResolutionDropdown : MonoBehaviour
	{
		private void Awake()
		{
			this._dropdown = base.GetComponent<TMP_Dropdown>();
			this.UpdateValues();
			DisplayVideoSettings.OnDisplayChanged += this.UpdateValues;
			UserSetting<int>.AddListener<DisplayVideoSetting>(DisplayVideoSetting.AspectRatio, new Action<int>(this.OnAspectRatioChanged));
		}

		private void OnDestroy()
		{
			DisplayVideoSettings.OnDisplayChanged -= this.UpdateValues;
			UserSetting<int>.RemoveListener<DisplayVideoSetting>(DisplayVideoSetting.AspectRatio, new Action<int>(this.OnAspectRatioChanged));
		}

		private void Update()
		{
			Resolution resolution = new Resolution
			{
				width = Screen.width,
				height = Screen.height
			};
			this._unsupportedRatioText.enabled = !DisplayVideoSettings.IsSupportedRatio(resolution);
		}

		private void UpdateValues()
		{
			this.OnAspectRatioChanged(UserSetting<int>.Get<DisplayVideoSetting>(DisplayVideoSetting.AspectRatio));
		}

		private void OnAspectRatioChanged(int selectedOption)
		{
			this._dropdown.ClearOptions();
			bool flag = false;
			foreach (Resolution resolution in DisplayVideoSettings.GetSelectedAspectResolutions(selectedOption))
			{
				flag = true;
				this._dropdown.options.Add(new TMP_Dropdown.OptionData(string.Format("{0} × {1}", resolution.width, resolution.height)));
			}
			this._dropdown.interactable = flag;
			if (!flag)
			{
				this._dropdown.options.Add(new TMP_Dropdown.OptionData(this._noResolutionsErrorMessage.DisplayText));
			}
			else
			{
				this._dropdown.SetValueWithoutNotify(UserSetting<int>.Get<DisplayVideoSetting>(DisplayVideoSetting.Resolution));
			}
			this._dropdown.RefreshShownValue();
		}

		[SerializeField]
		private TextLanguageReplacer _noResolutionsErrorMessage;

		[SerializeField]
		private TMP_Text _unsupportedRatioText;

		private TMP_Dropdown _dropdown;
	}
}
