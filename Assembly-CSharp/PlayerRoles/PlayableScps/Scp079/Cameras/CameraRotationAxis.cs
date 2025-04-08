using System;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;
using UserSettings.ControlsSettings;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	[Serializable]
	public class CameraRotationAxis : CameraAxisBase
	{
		public Transform Pivot
		{
			get
			{
				return this._pivot;
			}
		}

		private float MouseInput
		{
			get
			{
				return Input.GetAxisRaw(this._isVertical ? "Mouse Y" : "Mouse X") * 2f * SensitivitySettings.SensMultiplier;
			}
		}

		protected override float SpectatorLerpMultiplier
		{
			get
			{
				return 7.5f;
			}
		}

		internal override void Update(Scp079Camera cam)
		{
			base.Update(cam);
			if (!cam.IsActive || !cam.IsUsedByLocalPlayer || Scp079CursorManager.LockCameras)
			{
				return;
			}
			if (!Cursor.visible)
			{
				float num = this.MouseInput;
				if (this._isVertical && !SensitivitySettings.Invert)
				{
					num *= -1f;
				}
				num /= Mathf.LerpUnclamped(1f, cam.ZoomAxis.CurrentZoom, SensitivitySettings.AdsReductionMultiplier);
				base.TargetValue += num;
				return;
			}
			float num2;
			float num3;
			if (this._isVertical)
			{
				num2 = (float)Screen.height;
				num3 = num2 - Input.mousePosition.y;
			}
			else
			{
				num3 = Input.mousePosition.x;
				num2 = (float)Screen.width;
			}
			float num4 = num2 / 2f;
			float num5 = ((num3 < num4) ? (-1f) : 1f);
			float num6 = Mathf.Abs((num3 - num4) / num4);
			float num7 = num4 - Mathf.Abs(num3 - num4);
			float num8 = 150f * Time.deltaTime;
			num8 *= Mathf.InverseLerp(0.93f, 0.98f, num6);
			if (num7 <= 2f)
			{
				float num9 = this.MouseInput * num5;
				if (this._isVertical)
				{
					num9 *= -1f;
				}
				num8 += Mathf.Max(num9, 0f);
			}
			base.TargetValue += num8 * num5;
		}

		protected override void OnValueChanged(float newValue, Scp079Camera cam)
		{
			float num = (Scp079Role.LocalInstanceActive ? base.TargetValue : newValue);
			this._pivot.localRotation = (this._isVertical ? Quaternion.Euler(num, 0f, 0f) : Quaternion.Euler(0f, num, 0f));
		}

		internal override void Awake(Scp079Camera cam)
		{
			base.Awake(cam);
			Vector3 eulerAngles = this._pivot.localRotation.eulerAngles;
			float num;
			for (num = (this._isVertical ? eulerAngles.x : eulerAngles.y); num < base.MinValue; num += 360f)
			{
			}
			while (num > base.MaxValue)
			{
				num -= 360f;
			}
			base.TargetValue = num;
		}

		[SerializeField]
		private Transform _pivot;

		[SerializeField]
		private bool _isVertical;

		private const string HorizontalAxis = "Mouse X";

		private const string VerticalAxis = "Mouse Y";

		private const float OverallSensMultiplier = 2f;

		private const float MoveSpeed = 150f;

		private const float BeginMovePercent = 0.93f;

		private const float FullSpeedPercent = 0.98f;

		private const int EdgeThreshold = 2;
	}
}
