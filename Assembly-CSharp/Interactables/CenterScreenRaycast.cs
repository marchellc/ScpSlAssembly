using System;
using UnityEngine;

namespace Interactables;

public class CenterScreenRaycast : MonoBehaviour
{
	[SerializeField]
	private float _rayDistance = 300f;

	[SerializeField]
	private LayerMask _centerScreenRayHits;

	public static RaycastHit LastRaycastHit { get; private set; }

	public static event Action<RaycastHit> OnCenterRaycastHit;

	public static event Action OnCenterRaycastMissed;

	private void Awake()
	{
		MainCameraController.OnUpdated += PerformRaycast;
	}

	private void OnDestroy()
	{
		MainCameraController.OnUpdated -= PerformRaycast;
	}

	private void PerformRaycast()
	{
		Transform currentCamera = MainCameraController.CurrentCamera;
		if (Physics.Raycast(new Ray(currentCamera.position, currentCamera.forward), out var hitInfo, this._rayDistance, this._centerScreenRayHits))
		{
			CenterScreenRaycast.LastRaycastHit = hitInfo;
			CenterScreenRaycast.OnCenterRaycastHit?.Invoke(hitInfo);
		}
		else
		{
			CenterScreenRaycast.OnCenterRaycastMissed?.Invoke();
		}
	}
}
