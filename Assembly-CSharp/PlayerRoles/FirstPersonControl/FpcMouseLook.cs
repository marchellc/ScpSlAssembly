using System;
using CursorManagement;
using Mirror;
using RelativePositioning;
using UnityEngine;
using UserSettings.ControlsSettings;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcMouseLook
	{
		public FpcMouseLook(ReferenceHub hub, FirstPersonMovementModule fpmm)
		{
			this._hub = hub;
			this._fpmm = fpmm;
			this.CurrentHorizontal = hub.transform.eulerAngles.y;
			this.CurrentVertical = 0f;
			if (hub.isLocalPlayer)
			{
				FpcMouseLook._localInstance = this;
			}
		}

		public static bool TryGetLocalMouseLook(out FpcMouseLook ml)
		{
			ml = FpcMouseLook._localInstance;
			return ml != null;
		}

		public float CurrentHorizontal
		{
			get
			{
				return this._curHorizontal;
			}
			set
			{
				float num = this.ClampHorizontal(value);
				this._curHorizontal = num;
				this._inputHorizontal = num;
			}
		}

		public float CurrentVertical
		{
			get
			{
				return this._curVertical;
			}
			set
			{
				float num = this.ClampVertical(value);
				this._curVertical = num;
				this._inputVertical = num;
			}
		}

		private Quaternion TargetHubRotation
		{
			get
			{
				return Quaternion.Euler(Vector3.up * this._curHorizontal);
			}
		}

		private Quaternion TargetCamRotation
		{
			get
			{
				return Quaternion.Euler(Vector3.left * this._curVertical);
			}
		}

		public void UpdateRotation()
		{
			Quaternion quaternion;
			Quaternion quaternion2;
			if (this._hub.isLocalPlayer)
			{
				float num;
				float num2;
				this.GetMouseInput(out num, out num2);
				if (Cursor.visible || CursorManager.MovementLocked)
				{
					num = 0f;
					num2 = 0f;
				}
				this._inputVertical = this.ClampVertical(this._inputVertical + num2);
				this._inputHorizontal = this.ClampHorizontal(this._inputHorizontal + num);
				float num3 = (SensitivitySettings.SmoothInput ? (10f * Time.deltaTime) : 1f);
				this._curVertical = this.ClampVertical(Mathf.LerpAngle(this._curVertical, this._inputVertical, num3));
				this._curHorizontal = this.ClampHorizontal(Mathf.LerpAngle(this._curHorizontal, this._inputHorizontal, num3));
				quaternion = this.TargetHubRotation;
				quaternion2 = this.TargetCamRotation;
			}
			else
			{
				if (!NetworkServer.active || !this._hub.IsDummy)
				{
					this.CurrentHorizontal = WaypointBase.GetWorldRotation(this._fpmm.Motor.ReceivedPosition.WaypointId, Quaternion.Euler(Vector3.up * this._syncHorizontal)).eulerAngles.y;
					this.CurrentVertical = this._syncVertical;
				}
				float num4 = (NetworkServer.active ? 1f : (22f * Time.deltaTime));
				quaternion = Quaternion.Lerp(this._hub.transform.rotation, this.TargetHubRotation, num4);
				quaternion2 = Quaternion.Lerp(this._hub.PlayerCameraReference.localRotation, this.TargetCamRotation, num4);
			}
			this._hub.transform.rotation = quaternion;
			this._hub.PlayerCameraReference.localRotation = quaternion2;
		}

		public void GetMouseInput(out float hRot, out float vRot)
		{
			hRot = Input.GetAxisRaw("Mouse X");
			vRot = Input.GetAxisRaw("Mouse Y");
		}

		public void ApplySyncValues(ushort horizontal, ushort vertical)
		{
			if (this._prevSyncH == horizontal && this._prevSyncV == vertical)
			{
				this._fpmm.Motor.RotationDetected = false;
				return;
			}
			this._prevSyncH = horizontal;
			this._prevSyncV = vertical;
			this._syncHorizontal = Mathf.Lerp(0f, 360f, (float)horizontal / 65535f);
			this._syncVertical = Mathf.Lerp(-88f, 88f, (float)vertical / 65535f);
			if (this._hub.isLocalPlayer)
			{
				this.CurrentHorizontal = this._syncHorizontal;
				this.CurrentVertical = this._syncVertical;
			}
			this._fpmm.Motor.RotationDetected = true;
		}

		public void GetSyncValues(byte waypointId, out ushort syncH, out ushort syncV)
		{
			syncH = (ushort)Mathf.RoundToInt(Mathf.InverseLerp(0f, 360f, WaypointBase.GetRelativeRotation(waypointId, Quaternion.Euler(Vector3.up * this.CurrentHorizontal)).eulerAngles.y) * 65535f);
			syncV = (ushort)Mathf.RoundToInt(Mathf.InverseLerp(-88f, 88f, this.CurrentVertical) * 65535f);
		}

		protected virtual float ClampHorizontal(float f)
		{
			while (f < 0f)
			{
				f += 360f;
			}
			while (f > 360f)
			{
				f -= 360f;
			}
			return f;
		}

		protected virtual float ClampVertical(float f)
		{
			return Mathf.Clamp(f, -88f, 88f);
		}

		private static FpcMouseLook _localInstance;

		public const float MinimumVer = -88f;

		public const float MaximumVer = 88f;

		public const float OverallMultiplier = 2f;

		private const float FullAngle = 360f;

		private const float SmoothTime = 10f;

		private const float ThirdpersonSmooth = 22f;

		private readonly ReferenceHub _hub;

		private readonly FirstPersonMovementModule _fpmm;

		private float _curHorizontal;

		private float _curVertical;

		private float _syncHorizontal;

		private float _syncVertical;

		private float _inputHorizontal;

		private float _inputVertical;

		private ushort _prevSyncH;

		private ushort _prevSyncV;
	}
}
