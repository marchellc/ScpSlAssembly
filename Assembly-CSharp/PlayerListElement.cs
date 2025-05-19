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
		RefreshMute(instance, VoiceChatMutes.GetFlags(instance));
	}

	private void OnDestroy()
	{
		VoiceChatMutes.OnFlagsSet -= RefreshMute;
	}

	private void RefreshMute(ReferenceHub hub, VcMuteFlags flags)
	{
		if (!(hub != instance) && ReferenceHub.AllHubs.Contains(hub))
		{
			ToggleMute.isOn = flags != VcMuteFlags.None;
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
