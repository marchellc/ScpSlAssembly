using System;

namespace UserSettings.GUIElements
{
	public class CustomUserSettingsEntryDescription : UserSettingsEntryDescription
	{
		public override string Text
		{
			get
			{
				return this._customText;
			}
		}

		public void SetCustomText(string text)
		{
			this._customText = text;
		}

		private string _customText;
	}
}
