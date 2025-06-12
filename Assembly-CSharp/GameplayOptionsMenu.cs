using UnityEngine;
using UnityEngine.UI;

public class GameplayOptionsMenu : MonoBehaviour
{
	public Slider classIntroFastFadeSlider;

	public Slider headBobSlider;

	public Slider toggleSprintSlider;

	public Slider modeSwitchToggle079;

	public Slider postProcessing079;

	public Slider healthBarShowsExact;

	public Slider richPresence;

	public Slider publicLobby;

	public Slider hideIP;

	public Slider toggleSearch;

	private bool _isAwake;

	public void Awake()
	{
		this.classIntroFastFadeSlider.value = (PlayerPrefsSl.Get("ClassIntroFastFade", defaultValue: false) ? 1f : 0f);
		this.headBobSlider.value = (PlayerPrefsSl.Get("HeadBob", defaultValue: true) ? 1f : 0f);
		this.toggleSprintSlider.value = (PlayerPrefsSl.Get("ToggleSprint", defaultValue: false) ? 1f : 0f);
		this.modeSwitchToggle079.value = (PlayerPrefsSl.Get("ModeSwitchSetting079", defaultValue: false) ? 1f : 0f);
		this.postProcessing079.value = (PlayerPrefsSl.Get("PostProcessing079", defaultValue: true) ? 1f : 0f);
		this.healthBarShowsExact.value = (PlayerPrefsSl.Get("HealthBarShowsExact", defaultValue: true) ? 1f : 0f);
		this.richPresence.value = (PlayerPrefsSl.Get("RichPresence", defaultValue: true) ? 1f : 0f);
		this.publicLobby.value = (PlayerPrefsSl.Get("PublicLobby", defaultValue: true) ? 1f : 0f);
		this.hideIP.value = (PlayerPrefsSl.Get("HideIP", defaultValue: false) ? 1f : 0f);
		this.toggleSearch.value = (PlayerPrefsSl.Get("ToggleSearch", defaultValue: false) ? 1f : 0f);
		this._isAwake = true;
	}

	public void SaveSettings()
	{
		if (this._isAwake)
		{
			PlayerPrefsSl.Set("ClassIntroFastFade", (int)this.classIntroFastFadeSlider.value == 1);
			PlayerPrefsSl.Set("HeadBob", (int)this.headBobSlider.value == 1);
			PlayerPrefsSl.Set("ToggleSprint", (int)this.toggleSprintSlider.value == 1);
			PlayerPrefsSl.Set("ModeSwitchSetting079", (int)this.modeSwitchToggle079.value == 1);
			PlayerPrefsSl.Set("PostProcessing079", (int)this.postProcessing079.value == 1);
			PlayerPrefsSl.Set("HealthBarShowsExact", (int)this.healthBarShowsExact.value == 1);
			PlayerPrefsSl.Set("RichPresence", (int)this.richPresence.value == 1);
			PlayerPrefsSl.Set("PublicLobby", (int)this.publicLobby.value == 1);
			PlayerPrefsSl.Set("HideIP", (int)this.hideIP.value == 1);
			PlayerPrefsSl.Set("ToggleSearch", (int)this.toggleSearch.value == 1);
		}
	}
}
