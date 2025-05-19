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
			return _loudnessIndicator.fillAmount;
		}
		set
		{
			_loudnessIndicator.fillAmount = Mathf.Clamp01(value);
		}
	}

	private void Awake()
	{
		_singleton = this;
		_singletonSet = true;
		base.gameObject.SetActive(value: false);
		PlayerRoleManager.OnRoleChanged += UpdateColor;
	}

	private void OnDestroy()
	{
		_singletonSet = false;
		PlayerRoleManager.OnRoleChanged -= UpdateColor;
	}

	private void Update()
	{
		FillAmount -= Time.deltaTime * _dropSpeed;
	}

	private void UpdateColor(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (userHub.isLocalPlayer)
		{
			Color roleColor = newRole.RoleColor;
			UpdateColor(_outline, roleColor);
			UpdateColor(_loudnessIndicator, roleColor);
		}
	}

	private void UpdateColor(Graphic target, Color c)
	{
		target.color = new Color(c.r, c.g, c.b, target.color.a);
	}

	private void RefreshIndicator(bool isSpeaking, float loudness)
	{
		base.gameObject.SetActive(isSpeaking);
		float num = Mathf.Pow(Mathf.InverseLerp(_minValue, _maxValue, loudness), _curvePower);
		FillAmount = (isSpeaking ? Mathf.Max(FillAmount, num) : num);
	}

	public static void ShowIndicator(bool isSpeaking, float loudness)
	{
		if (_singletonSet)
		{
			_singleton.RefreshIndicator(isSpeaking, loudness);
		}
	}
}
