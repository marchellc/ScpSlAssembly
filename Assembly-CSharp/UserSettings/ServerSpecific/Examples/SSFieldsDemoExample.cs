using System;
using TMPro;
using UnityEngine;

namespace UserSettings.ServerSpecific.Examples
{
	public class SSFieldsDemoExample : SSExampleImplementationBase
	{
		public override string Name
		{
			get
			{
				return "All fields demo (no functionality)";
			}
		}

		public override void Activate()
		{
			string[] array = new string[] { "Option 1", "Option 2", "Option 3", "Option 4" };
			ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
			{
				new SSGroupHeader("GroupHeader", false, null),
				new SSTwoButtonsSetting(null, "TwoButtonsSetting", "Option A", "Option B", false, null),
				new SSTextArea(null, "TextArea", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "Multiline collapsable TextArea.\nLorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.", SSTextArea.FoldoutMode.ExtendedByDefault, null, TextAlignmentOptions.TopLeft),
				new SSSliderSetting(null, "SliderSetting", 0f, 1f, 0f, false, "0.##", "{0}", null),
				new SSPlaintextSetting(null, "Plaintext", "...", 64, TMP_InputField.ContentType.Standard, null),
				new SSKeybindSetting(null, "KeybindSetting", KeyCode.None, true, null),
				new SSDropdownSetting(null, "DropdownSetting", array, 0, SSDropdownSetting.DropdownEntryType.Regular, null),
				new SSDropdownSetting(null, "Scrollable DropdownSetting", array, 0, SSDropdownSetting.DropdownEntryType.Scrollable, null),
				new SSButton(null, "Button", "Press me!", null, null),
				new SSGroupHeader("Hints", false, "Group headers are used to separate settings into subcategories."),
				new SSTwoButtonsSetting(null, "Another TwoButtonsSetting", "Option A", "Option B", false, "Two Buttons are used to store Boolean values."),
				new SSSliderSetting(null, "Another SliderSetting", 0f, 1f, 0f, false, "0.##", "{0}", "Sliders store a numeric value within a defined range."),
				new SSPlaintextSetting(null, "Another Plaintext", "...", 64, TMP_InputField.ContentType.Standard, "Plaintext fields store any provided text."),
				new SSKeybindSetting(null, "Another KeybindSetting", KeyCode.None, true, "Allows checking if the player is currently holding the action key."),
				new SSDropdownSetting(null, "Another DropdownSetting", array, 0, SSDropdownSetting.DropdownEntryType.Regular, "Stores an integer value between 0 and the length of options minus 1."),
				new SSDropdownSetting(null, "Another Scrollable DropdownSetting", array, 0, SSDropdownSetting.DropdownEntryType.Scrollable, "Alternative to dropdown. API is the same as in regular dropdown, but the client-side entry behaves differently."),
				new SSButton(null, "Another Button", "Press me! (again)", null, "Triggers an event whenever it is pressed.")
			};
			ServerSpecificSettingsSync.SendToAll();
		}

		public override void Deactivate()
		{
		}
	}
}
