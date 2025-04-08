using System;
using PlayerRoles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoiceChat.Playbacks;

namespace VoiceChat
{
	public class GlobalChatIndicator : MonoBehaviour
	{
		public void Setup(IGlobalPlayback igp, ReferenceHub owner)
		{
			this._playback = igp;
			this._owner = owner;
			this._t = base.transform;
			this._noSpeakTime = 0f;
		}

		public void Refresh()
		{
			if (this._playback.GlobalChatActive)
			{
				if (!this._wasSpeaking)
				{
					base.gameObject.SetActive(true);
					this._t.SetAsLastSibling();
					this._wasSpeaking = true;
				}
				this._lastColor = this._playback.GlobalChatColor;
				this.SetColors(this._playback.GlobalChatLoudness);
				Texture texture;
				if (this.TryGetIcon(this._playback.GlobalChatIcon, this._owner, out texture))
				{
					this._icon.texture = texture;
					this._iconRoot.SetActive(true);
				}
				else
				{
					this._iconRoot.SetActive(false);
				}
				this._nickname.text = this._playback.GlobalChatName;
				return;
			}
			if (!this._wasSpeaking)
			{
				return;
			}
			this._noSpeakTime += Time.deltaTime;
			this.SetColors(0f);
			if (this._noSpeakTime < 0.3f)
			{
				return;
			}
			base.gameObject.SetActive(false);
			this._wasSpeaking = false;
		}

		private void SetColors(float loudness)
		{
			Graphic[] backgrounds = this._backgrounds;
			for (int i = 0; i < backgrounds.Length; i++)
			{
				backgrounds[i].color = Color.Lerp(Color.black, this._lastColor, loudness);
			}
			Outline[] outlines = this._outlines;
			for (int i = 0; i < outlines.Length; i++)
			{
				outlines[i].effectColor = Color.Lerp(this._lastColor, Color.white, loudness);
			}
		}

		private bool TryGetIcon(GlobalChatIconType icon, ReferenceHub owner, out Texture result)
		{
			result = null;
			switch (icon)
			{
			case GlobalChatIconType.None:
				return false;
			case GlobalChatIconType.Avatar:
				if (!(owner == null))
				{
					IAvatarRole avatarRole = owner.roleManager.CurrentRole as IAvatarRole;
					if (avatarRole != null)
					{
						result = avatarRole.RoleAvatar;
						return true;
					}
				}
				return false;
			case GlobalChatIconType.Radio:
				result = this._radioIcon;
				return true;
			case GlobalChatIconType.Intercom:
				result = this._intercomIcon;
				return true;
			default:
				return false;
			}
		}

		[SerializeField]
		private TextMeshProUGUI _nickname;

		[SerializeField]
		private RawImage _icon;

		[SerializeField]
		private Graphic[] _backgrounds;

		[SerializeField]
		private Outline[] _outlines;

		[SerializeField]
		private GameObject _iconRoot;

		[SerializeField]
		private Texture _radioIcon;

		[SerializeField]
		private Texture _intercomIcon;

		private IGlobalPlayback _playback;

		private ReferenceHub _owner;

		private bool _wasSpeaking;

		private float _noSpeakTime;

		private Color _lastColor;

		private Transform _t;

		private const float SustainTime = 0.3f;
	}
}
