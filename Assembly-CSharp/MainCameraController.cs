using System;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

public class MainCameraController : MonoBehaviour
{
	public static event Action OnUpdated;

	public static bool InstanceActive { get; private set; }

	public static Vector3 LastPosition { get; private set; }

	public static Quaternion LastRotation { get; private set; }

	public static Transform CurrentCamera
	{
		get
		{
			return MainCameraController._currentCamera;
		}
		set
		{
			if (MainCameraController._currentCamera == value)
			{
				return;
			}
			Transform transform = value;
			if (transform == null)
			{
				transform = MainCameraController._singleton._defaultCamera;
			}
			if (MainCameraController._currentCamera != null)
			{
				MainCameraController._currentCamera.gameObject.SetActive(false);
			}
			MainCameraController._currentCamera = transform;
			if (transform != null)
			{
				transform.gameObject.SetActive(true);
			}
		}
	}

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
		ReferenceHub referenceHub;
		if (ReferenceHub.TryGetLocalHub(out referenceHub))
		{
			ICameraController cameraController = referenceHub.roleManager.CurrentRole as ICameraController;
			if (cameraController != null)
			{
				pos = cameraController.CameraPosition;
				IAdvancedCameraController advancedCameraController = cameraController as IAdvancedCameraController;
				float num = ((advancedCameraController != null) ? advancedCameraController.RollRotation : 0f);
				rot = Quaternion.Euler(cameraController.VerticalRotation, cameraController.HorizontalRotation, num);
				return;
			}
		}
		pos = MainCameraController._defaultPos;
		rot = Quaternion.identity;
	}

	public static void ForceUpdatePosition()
	{
		if (!MainCameraController.InstanceActive)
		{
			return;
		}
		Vector3 vector;
		Quaternion quaternion;
		MainCameraController.GetPositionAndRotation(out vector, out quaternion);
		MainCameraController.CurrentCamera.SetPositionAndRotation(vector, quaternion);
		MainCameraController.LastPosition = vector;
		MainCameraController.LastRotation = quaternion;
		Action onUpdated = MainCameraController.OnUpdated;
		if (onUpdated == null)
		{
			return;
		}
		onUpdated();
	}

	public static bool TryGetCurrentRoom(out RoomIdentifier rid)
	{
		if (MainCameraController.CurrentCamera == null)
		{
			rid = null;
			return false;
		}
		Vector3Int vector3Int = RoomUtils.PositionToCoords(MainCameraController.CurrentCamera.position);
		return RoomIdentifier.RoomsByCoordinates.TryGetValue(vector3Int, out rid);
	}

	public const float DefaultVerticalFOV = 70f;

	private static Transform _currentCamera;

	private static MainCameraController _singleton;

	private static Vector3 _defaultPos;

	[SerializeField]
	private Transform _defaultCamera;
}
