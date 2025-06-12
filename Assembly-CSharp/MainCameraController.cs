using System;
using PlayerRoles;
using UnityEngine;

public class MainCameraController : MonoBehaviour
{
	public const float DefaultVerticalFOV = 70f;

	private static Transform _currentCamera;

	private static MainCameraController _singleton;

	private static Vector3 _defaultPos;

	[SerializeField]
	private Transform _defaultCamera;

	public static bool InstanceActive { get; private set; }

	public static Vector3 LastPosition { get; private set; }

	public static Quaternion LastRotation { get; private set; }

	public static Ray LastForwardRay => new Ray(MainCameraController.LastPosition, MainCameraController.LastRotation * Vector3.forward);

	public static Transform CurrentCamera
	{
		get
		{
			return MainCameraController._currentCamera;
		}
		set
		{
			if (!(MainCameraController._currentCamera == value))
			{
				Transform transform = value;
				if (transform == null)
				{
					transform = MainCameraController._singleton._defaultCamera;
				}
				if (MainCameraController._currentCamera != null)
				{
					MainCameraController._currentCamera.gameObject.SetActive(value: false);
				}
				MainCameraController._currentCamera = transform;
				if (transform != null)
				{
					transform.gameObject.SetActive(value: true);
				}
			}
		}
	}

	public static event Action OnUpdated;

	public static event Action OnBeforeUpdated;

	private void Awake()
	{
		MainCameraController.InstanceActive = true;
		MainCameraController._singleton = this;
		MainCameraController._defaultPos = this._defaultCamera.position;
		MainCameraController.CurrentCamera = this._defaultCamera;
	}

	private void OnDestroy()
	{
		MainCameraController.InstanceActive = false;
	}

	private void LateUpdate()
	{
		MainCameraController.ForceUpdatePosition();
	}

	private static void GetPositionAndRotation(out Vector3 pos, out Quaternion rot)
	{
		if (ReferenceHub.TryGetLocalHub(out var hub) && hub.roleManager.CurrentRole is ICameraController cameraController)
		{
			pos = cameraController.CameraPosition;
			float z = ((cameraController is IAdvancedCameraController advancedCameraController) ? advancedCameraController.RollRotation : 0f);
			rot = Quaternion.Euler(cameraController.VerticalRotation, cameraController.HorizontalRotation, z);
		}
		else
		{
			pos = MainCameraController._defaultPos;
			rot = Quaternion.identity;
		}
	}

	public static void ForceUpdatePosition()
	{
		if (MainCameraController.InstanceActive)
		{
			MainCameraController.OnBeforeUpdated?.Invoke();
			MainCameraController.GetPositionAndRotation(out var pos, out var rot);
			MainCameraController.CurrentCamera.SetPositionAndRotation(pos, rot);
			MainCameraController.LastPosition = pos;
			MainCameraController.LastRotation = rot;
			MainCameraController.OnUpdated?.Invoke();
		}
	}
}
