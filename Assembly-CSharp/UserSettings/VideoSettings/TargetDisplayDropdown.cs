using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UserSettings.VideoSettings;

public class TargetDisplayDropdown : MonoBehaviour
{
	[SerializeField]
	private int _fontSize;

	private TMP_Dropdown _dropdown;

	private int _prevCount;

	private const string NameFormat = "{0} <size={1}>({2} × {3} - {4} Hz)</size>";

	private static readonly List<DisplayInfo> Displays = new List<DisplayInfo>();

	private void Awake()
	{
		this._dropdown = base.GetComponent<TMP_Dropdown>();
		this._dropdown.onValueChanged.AddListener(DisplayVideoSettings.ChangeDisplay);
	}

	private void OnEnable()
	{
		this.RefreshValue();
		DisplayVideoSettings.OnDisplayChanged += RefreshValue;
	}

	private void OnDisable()
	{
		DisplayVideoSettings.OnDisplayChanged -= RefreshValue;
	}

	private void RefreshValue()
	{
		Screen.GetDisplayLayout(TargetDisplayDropdown.Displays);
		this.UpdateOptions();
		this._dropdown.SetValueWithoutNotify(DisplayVideoSettings.CurrentDisplayIndex);
	}

	private void UpdateOptions()
	{
		List<TMP_Dropdown.OptionData> options = this._dropdown.options;
		int count = TargetDisplayDropdown.Displays.Count;
		int num = options.Count;
		for (int i = 0; i < count; i++)
		{
			DisplayInfo displayInfo = TargetDisplayDropdown.Displays[i];
			string text = $"{displayInfo.name} <size={this._fontSize}>({displayInfo.width} × {displayInfo.height} - {displayInfo.refreshRate.value} Hz)</size>";
			if (i < num)
			{
				options[i].text = text;
				continue;
			}
			options.Add(new TMP_Dropdown.OptionData(text));
			num++;
		}
		int num2 = num - count;
		if (num2 > 0)
		{
			options.RemoveRange(count, num2);
		}
		this._dropdown.interactable = count > 1;
		this._dropdown.RefreshShownValue();
	}

	private void Update()
	{
		Screen.GetDisplayLayout(TargetDisplayDropdown.Displays);
		if (this._prevCount != TargetDisplayDropdown.Displays.Count)
		{
			this.UpdateOptions();
			this._prevCount = TargetDisplayDropdown.Displays.Count;
		}
	}
}
