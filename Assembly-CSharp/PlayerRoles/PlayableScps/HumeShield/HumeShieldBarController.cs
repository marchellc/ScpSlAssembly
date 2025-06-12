using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.HumeShield;

public class HumeShieldBarController : MonoBehaviour
{
	[SerializeField]
	private StatusBar _targetBar;

	[SerializeField]
	private Image _hsWarning;

	[SerializeField]
	private Color _hsColor;

	private float _colorTimer;

	private bool _prevVisible;

	private bool _firstFrame;

	private const float FadeSpeed = 8f;

	private const float BlinkSpeed = 35f;

	private void Awake()
	{
		this._targetBar.AutohideOption = StatusBar.AutoHideType.AlwaysVisible;
		this._firstFrame = true;
		if (ReferenceHub.TryGetPovHub(out var hub) && hub.roleManager.CurrentRole is IHumeShieldedRole humeShieldedRole && humeShieldedRole.HumeShieldModule.HideWhenEmpty)
		{
			this._targetBar.AutohideOption = StatusBar.AutoHideType.WhenEmpty;
		}
	}

	private void Update()
	{
		this.GetValues(out var barVisible, out var warningColor);
		if (this._firstFrame || barVisible != this._prevVisible)
		{
			this._targetBar.SetAlpha(barVisible ? 1 : 0);
			this._firstFrame = true;
			this._prevVisible = barVisible;
		}
		if (warningColor.HasValue)
		{
			Color value = warningColor.Value;
			Color hsColor = this._hsColor;
			float num = 35f * value.a;
			hsColor.a = (value.a = Mathf.Min(1f, this._colorTimer * 8f));
			float t = (Mathf.Sin(this._colorTimer * num) + 1f) / 2f;
			this._hsWarning.color = Color.Lerp(hsColor, value, t);
			this._colorTimer += Time.deltaTime;
		}
		else
		{
			Color color = this._hsWarning.color;
			color.a = Mathf.Max(0f, color.a - Time.deltaTime * 8f);
			this._hsWarning.color = color;
			this._colorTimer = 0f;
		}
	}

	private void GetValues(out bool barVisible, out Color? warningColor)
	{
		barVisible = false;
		warningColor = null;
		if (ReferenceHub.TryGetPovHub(out var hub))
		{
			IHumeShieldProvider.GetForHub(hub, out var stat, out barVisible, out var _, out var _, out warningColor);
			barVisible |= stat.CurValue > 0f;
		}
	}
}
