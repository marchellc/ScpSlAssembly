using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoiceChat;

public class PlayerListElement : MonoBehaviour
{
	private void Start()
	{
		VoiceChatMutes.OnFlagsSet += this.RefreshMute;
		this.RefreshMute(this.instance, VoiceChatMutes.GetFlags(this.instance));
	}

	private void OnDestroy()
	{
		VoiceChatMutes.OnFlagsSet -= this.RefreshMute;
	}

	private void RefreshMute(ReferenceHub hub, VcMuteFlags flags)
	{
		if (hub != this.instance)
		{
			return;
		}
		if (!ReferenceHub.AllHubs.Contains(hub))
		{
			return;
		}
		this.ToggleMute.isOn = flags > VcMuteFlags.None;
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

	public ReferenceHub instance;

	public TextMeshProUGUI TextNick;

	public TextMeshProUGUI TextBadge;

	public RawImage ImgVerified;

	public Image ImgBackground;

	public Toggle ToggleMute;

	public GameObject OpenProfile;
}
