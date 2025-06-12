namespace UserSettings.GUIElements;

public class CustomUserSettingsEntryDescription : UserSettingsEntryDescription
{
	private string _customText;

	public override string Text => this._customText;

	public void SetCustomText(string text)
	{
		this._customText = text;
	}
}
