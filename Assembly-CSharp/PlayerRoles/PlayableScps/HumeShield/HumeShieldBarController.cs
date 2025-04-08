using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.HumeShield
{
	public class HumeShieldBarController : MonoBehaviour
	{
		private void Awake()
		{
			this._targetBar.AutohideOption = StatusBar.AutoHideType.AlwaysVisible;
			this._firstFrame = true;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetPovHub(out referenceHub))
			{
				return;
			}
			IHumeShieldedRole humeShieldedRole = referenceHub.roleManager.CurrentRole as IHumeShieldedRole;
			if (humeShieldedRole == null)
			{
				return;
			}
			if (humeShieldedRole.HumeShieldModule.HideWhenEmpty)
			{
				this._targetBar.AutohideOption = StatusBar.AutoHideType.WhenEmpty;
			}
		}

		private void Update()
		{
			bool flag;
			Color? color;
			this.GetValues(out flag, out color);
			if (this._firstFrame || flag != this._prevVisible)
			{
				this._targetBar.SetAlpha((float)(flag ? 1 : 0));
				this._firstFrame = true;
				this._prevVisible = flag;
			}
			if (color != null)
			{
				Color value = color.Value;
				Color hsColor = this._hsColor;
				float num = 35f * value.a;
				float num2 = Mathf.Min(1f, this._colorTimer * 8f);
				value.a = num2;
				hsColor.a = num2;
				float num3 = (Mathf.Sin(this._colorTimer * num) + 1f) / 2f;
				this._hsWarning.color = Color.Lerp(hsColor, value, num3);
				this._colorTimer += Time.deltaTime;
				return;
			}
			Color color2 = this._hsWarning.color;
			color2.a = Mathf.Max(0f, color2.a - Time.deltaTime * 8f);
			this._hsWarning.color = color2;
			this._colorTimer = 0f;
		}

		private void GetValues(out bool barVisible, out Color? warningColor)
		{
			barVisible = false;
			warningColor = null;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetPovHub(out referenceHub))
			{
				return;
			}
			IHumeShieldedRole humeShieldedRole = referenceHub.roleManager.CurrentRole as IHumeShieldedRole;
			if (humeShieldedRole == null)
			{
				return;
			}
			barVisible = !humeShieldedRole.HumeShieldModule.HideWhenEmpty || humeShieldedRole.HumeShieldModule.HsCurrent > 0f;
			warningColor = humeShieldedRole.HumeShieldModule.HsWarningColor;
		}

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
	}
}
