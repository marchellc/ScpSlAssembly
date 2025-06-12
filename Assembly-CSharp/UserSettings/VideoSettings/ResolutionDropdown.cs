using TMPro;
using UnityEngine;

namespace UserSettings.VideoSettings;

public class ResolutionDropdown : MonoBehaviour
{
	[SerializeField]
	private TextLanguageReplacer _noResolutionsErrorMessage;

	[SerializeField]
	private TMP_Text _unsupportedRatioText;

	private TMP_Dropdown _dropdown;

	private void Awake()
	{
		this._dropdown = base.GetComponent<TMP_Dropdown>();
		this.UpdateValues();
		DisplayVideoSettings.OnDisplayChanged += UpdateValues;
		UserSetting<int>.AddListener(DisplayVideoSetting.AspectRatio, OnAspectRatioChanged);
	}

	private void OnDestroy()
	{
		DisplayVideoSettings.OnDisplayChanged -= UpdateValues;
		UserSetting<int>.RemoveListener(DisplayVideoSetting.AspectRatio, OnAspectRatioChanged);
	}

	private void Update()
	{
		Resolution res = new Resolution
		{
			width = Screen.width,
			height = Screen.height
		};
		this._unsupportedRatioText.enabled = !DisplayVideoSettings.IsSupportedRatio(res);
	}

	private void UpdateValues()
	{
		this.OnAspectRatioChanged(UserSetting<int>.Get(DisplayVideoSetting.AspectRatio));
	}

	private void OnAspectRatioChanged(int selectedOption)
	{
		this._dropdown.ClearOptions();
		bool flag = false;
		Resolution[] selectedAspectResolutions = DisplayVideoSettings.GetSelectedAspectResolutions(selectedOption);
		for (int i = 0; i < selectedAspectResolutions.Length; i++)
		{
			Resolution resolution = selectedAspectResolutions[i];
			flag = true;
			this._dropdown.options.Add(new TMP_Dropdown.OptionData($"{resolution.width} × {resolution.height}"));
		}
		this._dropdown.interactable = flag;
		if (!flag)
		{
			this._dropdown.options.Add(new TMP_Dropdown.OptionData(this._noResolutionsErrorMessage.DisplayText));
		}
		else
		{
			this._dropdown.SetValueWithoutNotify(UserSetting<int>.Get(DisplayVideoSetting.Resolution));
		}
		this._dropdown.RefreshShownValue();
	}
}
