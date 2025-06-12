using System;
using CursorManagement;
using InventorySystem.Items;
using Mirror;
using NetworkManagerUtils.Dummies;
using RelativePositioning;
using UnityEngine;
using UserSettings.ControlsSettings;

namespace PlayerRoles.FirstPersonControl;

public class FpcMouseLook : IDummyActionProvider
{
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

	protected virtual float BiaxialSensitivity
	{
		get
		{
			float num = SensitivitySettings.SensMultiplier * 2f;
			if (this._hub.inventory.CurInstance is IZoomModifyingItem zoomModifyingItem && zoomModifyingItem != null)
			{
				num *= zoomModifyingItem.SensitivityScale;
			}
			return num;
		}
	}

	public float CurrentHorizontal
	{
		get
		{
			return this._curHorizontal;
		}
		set
		{
			this._inputHorizontal = (this._curHorizontal = this.ClampHorizontal(value));
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
			this._inputVertical = (this._curVertical = this.ClampVertical(value));
		}
	}

	private Quaternion TargetHubRotation => Quaternion.Euler(Vector3.up * this._curHorizontal);

	private Quaternion TargetCamRotation => Quaternion.Euler(Vector3.left * this._curVertical);

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

	public void UpdateRotation()
	{
		Quaternion rotation;
		Quaternion localRotation;
		if (this._hub.isLocalPlayer)
		{
			this.GetMouseInput(out var hRot, out var vRot);
			if (Cursor.visible || CursorManager.MovementLocked)
			{
				hRot = 0f;
				vRot = 0f;
			}
			this._inputVertical = this.ClampVertical(this._inputVertical + vRot);
			this._inputHorizontal = this.ClampHorizontal(this._inputHorizontal + hRot);
			float t = (SensitivitySettings.SmoothInput ? (10f * Time.deltaTime) : 1f);
			this._curVertical = this.ClampVertical(Mathf.LerpAngle(this._curVertical, this._inputVertical, t));
			this._curHorizontal = this.ClampHorizontal(Mathf.LerpAngle(this._curHorizontal, this._inputHorizontal, t));
			rotation = this.TargetHubRotation;
			localRotation = this.TargetCamRotation;
		}
		else
		{
			if (!NetworkServer.active || !this._hub.IsDummy)
			{
				this.CurrentHorizontal = WaypointBase.GetWorldRotation(this._fpmm.Motor.ReceivedPosition.WaypointId, Quaternion.Euler(Vector3.up * this._syncHorizontal)).eulerAngles.y;
				this.CurrentVertical = this._syncVertical;
			}
			float t2 = (NetworkServer.active ? 1f : (22f * Time.deltaTime));
			rotation = Quaternion.Lerp(this._hub.transform.rotation, this.TargetHubRotation, t2);
			localRotation = Quaternion.Lerp(this._hub.PlayerCameraReference.localRotation, this.TargetCamRotation, t2);
		}
		this._hub.transform.rotation = rotation;
		this._hub.PlayerCameraReference.localRotation = localRotation;
	}

	public void GetMouseInput(out float hRot, out float vRot)
	{
		hRot = Input.GetAxisRaw("Mouse X");
		vRot = Input.GetAxisRaw("Mouse Y");
		float biaxialSensitivity = this.BiaxialSensitivity;
		hRot = this.ProcessHorizontalInput(hRot * biaxialSensitivity);
		vRot = this.ProcessVerticalInput(vRot * biaxialSensitivity);
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
		this._syncHorizontal = Mathf.Lerp(0f, 360f, (float)(int)horizontal / 65535f);
		this._syncVertical = Mathf.Lerp(-88f, 88f, (float)(int)vertical / 65535f);
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

	protected virtual float ProcessVerticalInput(float f)
	{
		if (!SensitivitySettings.Invert)
		{
			return f;
		}
		return 0f - f;
	}

	protected virtual float ProcessHorizontalInput(float f)
	{
		return f;
	}

	public void PopulateDummyActions(Action<DummyAction> actionAdder)
	{
		int[] array = new int[4] { 1, 10, 45, 180 };
		foreach (int i2 in array)
		{
			actionAdder(new DummyAction(string.Format("{0}-{1}", "CurrentHorizontal", i2), delegate
			{
				this.CurrentHorizontal -= (float)i2;
			}));
			actionAdder(new DummyAction(string.Format("{0}+{1}", "CurrentHorizontal", i2), delegate
			{
				this.CurrentHorizontal += (float)i2;
			}));
			actionAdder(new DummyAction(string.Format("{0}-{1}", "CurrentVertical", i2), delegate
			{
				this.CurrentVertical -= (float)i2;
			}));
			actionAdder(new DummyAction(string.Format("{0}+{1}", "CurrentVertical", i2), delegate
			{
				this.CurrentVertical += (float)i2;
			}));
		}
	}
}
