using System;
using CursorManagement;
using PlayerRoles.FirstPersonControl;
using TMPro;
using UnityEngine;

namespace PlayerRoles.Filmmaker
{
	public class FilmmakerFly : MonoBehaviour, ICursorOverride
	{
		public static bool IsFlying { get; private set; }

		public CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.NoOverride;
			}
		}

		public bool LockMovement
		{
			get
			{
				return false;
			}
		}

		private bool WantsToFly
		{
			get
			{
				return Input.GetKey(this._keyFly);
			}
		}

		private void Start()
		{
			CursorManager.Register(this);
			FilmmakerFly._role = ReferenceHub.LocalHub.roleManager.CurrentRole as FilmmakerRole;
			FilmmakerFly._keyFwd = NewInput.GetKey(ActionName.MoveForward, KeyCode.None);
			FilmmakerFly._keyBwd = NewInput.GetKey(ActionName.MoveBackward, KeyCode.None);
			FilmmakerFly._keyLft = NewInput.GetKey(ActionName.MoveLeft, KeyCode.None);
			FilmmakerFly._keyRgt = NewInput.GetKey(ActionName.MoveRight, KeyCode.None);
			FilmmakerFly._keyUpw = NewInput.GetKey(ActionName.Jump, KeyCode.None);
			FilmmakerFly._keyDnw = NewInput.GetKey(ActionName.Sneak, KeyCode.None);
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
			Vector3 vector = Vector3.zero;
			if (Input.GetKey(FilmmakerFly._keyFwd))
			{
				vector += currentCamera.forward;
			}
			if (Input.GetKey(FilmmakerFly._keyBwd))
			{
				vector -= currentCamera.forward;
			}
			if (Input.GetKey(FilmmakerFly._keyRgt))
			{
				vector += currentCamera.right;
			}
			if (Input.GetKey(FilmmakerFly._keyLft))
			{
				vector -= currentCamera.right;
			}
			if (Input.GetKey(FilmmakerFly._keyUpw))
			{
				vector += currentCamera.up;
			}
			if (Input.GetKey(FilmmakerFly._keyDnw))
			{
				vector -= currentCamera.up;
			}
			FilmmakerFly._role.CameraPosition += movementSpeed * Time.deltaTime * vector;
		}

		private void UpdateRotation(float rollStep)
		{
			float num;
			float num2;
			FilmmakerFly._mLook.GetMouseInput(out num, out num2);
			FilmmakerFly._role.HorizontalRotation += num;
			FilmmakerFly._role.VerticalRotation -= num2;
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
			if (axisRaw == 0f)
			{
				return;
			}
			FilmmakerRole.ZoomScale = Mathf.Max(this._minZoom, FilmmakerRole.ZoomScale + Mathf.Sign(axisRaw) * step);
		}

		[SerializeField]
		private FilmmakerFly.PrecisionMode[] _precisionModes;

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

		[Serializable]
		private struct PrecisionMode
		{
			public string Name;

			public float MovementSpeed;

			public float ZoomSize;

			public float RollAmount;
		}
	}
}
