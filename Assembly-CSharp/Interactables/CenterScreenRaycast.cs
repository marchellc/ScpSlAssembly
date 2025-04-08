using System;
using UnityEngine;

namespace Interactables
{
	public class CenterScreenRaycast : MonoBehaviour
	{
		public static event Action<RaycastHit> OnCenterRaycastHit;

		public static event Action OnCenterRaycastMissed;

		private void Awake()
		{
			MainCameraController.OnUpdated += this.PerformRaycast;
		}

		private void OnDestroy()
		{
			MainCameraController.OnUpdated -= this.PerformRaycast;
		}

		private void PerformRaycast()
		{
			Transform currentCamera = MainCameraController.CurrentCamera;
			RaycastHit raycastHit;
			if (Physics.Raycast(new Ray(currentCamera.position, currentCamera.forward), out raycastHit, this._rayDistance, this._centerScreenRayHits))
			{
				Action<RaycastHit> onCenterRaycastHit = CenterScreenRaycast.OnCenterRaycastHit;
				if (onCenterRaycastHit == null)
				{
					return;
				}
				onCenterRaycastHit(raycastHit);
				return;
			}
			else
			{
				Action onCenterRaycastMissed = CenterScreenRaycast.OnCenterRaycastMissed;
				if (onCenterRaycastMissed == null)
				{
					return;
				}
				onCenterRaycastMissed();
				return;
			}
		}

		[SerializeField]
		private float _rayDistance = 300f;

		[SerializeField]
		private LayerMask _centerScreenRayHits;
	}
}
