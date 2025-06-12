using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoiceChat;

public class PlayerListElement : MonoBehaviour
{
	public ReferenceHub instance;

	public TextMeshProUGUI TextNick;

	public TextMeshProUGUI TextBadge;

	public RawImage ImgVerified;

	public Image ImgBackground;

	public Toggle ToggleMute;

	public GameObject OpenProfile;

	private void Start()
	{
		VoiceChatMutes.OnFlagsSet += RefreshMute;
		this.RefreshMute(this.instance, VoiceChatMutes.GetFlags(this.instance));
	}

	private void OnDestroy()
	{
		VoiceChatMutes.OnFlagsSet -= RefreshMute;
	}

	private void RefreshMute(ReferenceHub hub, VcMuteFlags flags)
	{
		if (!(hub != this.instance) && ReferenceHub.AllHubs.Contains(hub))
		{
			this.ToggleMute.isOn = flags != VcMuteFlags.None;
		}
	}

	public void Mute(bool b)
	{
	}

	public void OpenSteamAccount()
	{
	}

	public void Report()
	{
	}
}
