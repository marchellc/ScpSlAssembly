using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceChat;

public class VoiceChatMicrophoneIndicator : MonoBehaviour
{
	[SerializeField]
	private Image _outline;

	[SerializeField]
	private Image _loudnessIndicator;

	[SerializeField]
	private float _minValue;

	[SerializeField]
	private float _maxValue;

	[SerializeField]
	private float _dropSpeed;

	[SerializeField]
	private float _curvePower;

	private static VoiceChatMicrophoneIndicator _singleton;

	private static bool _singletonSet;

	private float FillAmount
	{
		get
		{
			return this._loudnessIndicator.fillAmount;
		}
		set
		{
			this._loudnessIndicator.fillAmount = Mathf.Clamp01(value);
		}
	}

	private void Awake()
	{
		VoiceChatMicrophoneIndicator._singleton = this;
		VoiceChatMicrophoneIndicator._singletonSet = true;
		base.gameObject.SetActive(value: false);
		PlayerRoleManager.OnRoleChanged += UpdateColor;
	}

	private void OnDestroy()
	{
		VoiceChatMicrophoneIndicator._singletonSet = false;
		PlayerRoleManager.OnRoleChanged -= UpdateColor;
	}

	private void Update()
	{
		this.FillAmount -= Time.deltaTime * this._dropSpeed;
	}

	private void UpdateColor(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (userHub.isLocalPlayer)
		{
			Color roleColor = newRole.RoleColor;
			this.UpdateColor(this._outline, roleColor);
			this.UpdateColor(this._loudnessIndicator, roleColor);
		}
	}

	private void UpdateColor(Graphic target, Color c)
	{
		target.color = new Color(c.r, c.g, c.b, target.color.a);
	}

	private void RefreshIndicator(bool isSpeaking, float loudness)
	{
		base.gameObject.SetActive(isSpeaking);
		float num = Mathf.Pow(Mathf.InverseLerp(this._minValue, this._maxValue, loudness), this._curvePower);
		this.FillAmount = (isSpeaking ? Mathf.Max(this.FillAmount, num) : num);
	}

	public static void ShowIndicator(bool isSpeaking, float loudness)
	{
		if (VoiceChatMicrophoneIndicator._singletonSet)
		{
			VoiceChatMicrophoneIndicator._singleton.RefreshIndicator(isSpeaking, loudness);
		}
	}
}
