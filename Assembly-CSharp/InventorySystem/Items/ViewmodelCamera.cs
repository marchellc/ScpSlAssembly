using System;
using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items
{
	public class ViewmodelCamera : MonoBehaviour
	{
		private void ResetCam()
		{
			this._resetting = true;
		}

		private void Awake()
		{
			ViewmodelCamera._camSet = true;
			ViewmodelCamera._viewModelCamera = base.GetComponent<Camera>();
			SpectatorTargetTracker.OnTargetChanged += this.ResetCam;
		}

		private void OnDestroy()
		{
			ViewmodelCamera._camSet = false;
			SpectatorTargetTracker.OnTargetChanged -= this.ResetCam;
		}

		private void LateUpdate()
		{
			float num;
			if (this.TryGetViewmodelFov(out num) && !this._resetting)
			{
				ViewmodelCamera._viewModelCamera.fieldOfView = num;
				return;
			}
			this._resetting = false;
		}

		private bool TryGetViewmodelFov(out float fov)
		{
			fov = ViewmodelCamera._viewModelCamera.fieldOfView;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return false;
			}
			IViewmodelRole viewmodelRole = referenceHub.roleManager.CurrentRole as IViewmodelRole;
			if (viewmodelRole == null)
			{
				return false;
			}
			float num;
			if (!viewmodelRole.TryGetViewmodelFov(out num))
			{
				return false;
			}
			fov = num;
			return true;
		}

		public static bool TryGetViewportPoint(Vector3 worldPos, out Vector3 viewport)
		{
			if (!ViewmodelCamera._camSet)
			{
				viewport = Vector3.zero;
				return false;
			}
			viewport = ViewmodelCamera._viewModelCamera.WorldToViewportPoint(worldPos);
			return true;
		}

		private bool _resetting;

		private static Camera _viewModelCamera;

		private static bool _camSet;
	}
}
