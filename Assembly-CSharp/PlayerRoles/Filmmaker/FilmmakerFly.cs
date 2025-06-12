using System;
using CursorManagement;
using PlayerRoles.FirstPersonControl;
using TMPro;
using UnityEngine;

namespace PlayerRoles.Filmmaker;

public class FilmmakerFly : MonoBehaviour, ICursorOverride
{
	[Serializable]
	private struct PrecisionMode
	{
		public string Name;

		public float MovementSpeed;

		public float ZoomSize;

		public float RollAmount;
	}

	[SerializeField]
	private PrecisionMode[] _precisionModes;

	[SerializeField]
	private TMP_Text _userInfo;

	[SerializeField]
	private float _minZoom;

	[SerializeField]
	private KeyCode _keyFly;

	[SerializeField]
	private KeyCode _keyChangePrecMode;

	[SerializeField]
	private KeyCode _keyRollLeft;

	[SerializeField]
	private KeyCode _keyRollRight;

	private static KeyCode _keyFwd;

	private static KeyCode _keyBwd;

	private static KeyCode _keyRgt;

	private static KeyCode _keyLft;

	private static KeyCode _keyUpw;

	private static KeyCode _keyDnw;

	private static FpcMouseLook _mLook;

	private static FilmmakerRole _role;

	private static int _curPrecisionMode;

	public static bool IsFlying { get; private set; }

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement => false;

	private bool WantsToFly => Input.GetKey(this._keyFly);

	private void Start()
	{
		CursorManager.Register(this);
		FilmmakerFly._role = ReferenceHub.LocalHub.roleManager.CurrentRole as FilmmakerRole;
		FilmmakerFly._keyFwd = NewInput.GetKey(ActionName.MoveForward);
		FilmmakerFly._keyBwd = NewInput.GetKey(ActionName.MoveBackward);
		FilmmakerFly._keyLft = NewInput.GetKey(ActionName.MoveLeft);
		FilmmakerFly._keyRgt = NewInput.GetKey(ActionName.MoveRight);
		FilmmakerFly._keyUpw = NewInput.GetKey(ActionName.Jump);
		FilmmakerFly._keyDnw = NewInput.GetKey(ActionName.Sneak);
		FilmmakerFly._mLook = new FpcMouseLook(ReferenceHub.LocalHub, null);
	}

	private void OnDestroy()
	{
		CursorManager.Unregister(this);
	}

	private void Update()
	{
	}

	private void UpdateMovement(float movementSpeed)
	{
		Transform currentCamera = MainCameraController.CurrentCamera;
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(FilmmakerFly._keyFwd))
		{
			zero += currentCamera.forward;
		}
		if (Input.GetKey(FilmmakerFly._keyBwd))
		{
			zero -= currentCamera.forward;
		}
		if (Input.GetKey(FilmmakerFly._keyRgt))
		{
			zero += currentCamera.right;
		}
		if (Input.GetKey(FilmmakerFly._keyLft))
		{
			zero -= currentCamera.right;
		}
		if (Input.GetKey(FilmmakerFly._keyUpw))
		{
			zero += currentCamera.up;
		}
		if (Input.GetKey(FilmmakerFly._keyDnw))
		{
			zero -= currentCamera.up;
		}
		FilmmakerFly._role.CameraPosition += movementSpeed * Time.deltaTime * zero;
	}

	private void UpdateRotation(float rollStep)
	{
		FilmmakerFly._mLook.GetMouseInput(out var hRot, out var vRot);
		FilmmakerFly._role.HorizontalRotation += hRot;
		FilmmakerFly._role.VerticalRotation -= vRot;
		if (Input.GetKeyDown(this._keyRollLeft))
		{
			FilmmakerFly._role.RollRotation += rollStep;
		}
		if (Input.GetKeyDown(this._keyRollRight))
		{
			FilmmakerFly._role.RollRotation -= rollStep;
		}
	}

	private void UpdateZoom(float step)
	{
		float axisRaw = Input.GetAxisRaw("Mouse ScrollWheel");
		if (axisRaw != 0f)
		{
			FilmmakerRole.ZoomScale = Mathf.Max(this._minZoom, FilmmakerRole.ZoomScale + Mathf.Sign(axisRaw) * step);
		}
	}
}
