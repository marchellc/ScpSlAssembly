using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items;

public class ViewmodelCamera : MonoBehaviour
{
	private bool _resetting;

	private static Camera _viewModelCamera;

	private static bool _camSet;

	private void ResetCam()
	{
		this._resetting = true;
	}

	private void Awake()
	{
		ViewmodelCamera._camSet = true;
		ViewmodelCamera._viewModelCamera = base.GetComponent<Camera>();
		SpectatorTargetTracker.OnTargetChanged += ResetCam;
	}

	private void OnDestroy()
	{
		ViewmodelCamera._camSet = false;
		SpectatorTargetTracker.OnTargetChanged -= ResetCam;
	}

	private void LateUpdate()
	{
		if (this.TryGetViewmodelFov(out var fov) && !this._resetting)
		{
			ViewmodelCamera._viewModelCamera.fieldOfView = fov;
		}
		else
		{
			this._resetting = false;
		}
	}

	private bool TryGetViewmodelFov(out float fov)
	{
		fov = ViewmodelCamera._viewModelCamera.fieldOfView;
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is IViewmodelRole viewmodelRole))
		{
			return false;
		}
		if (!viewmodelRole.TryGetViewmodelFov(out var fov2))
		{
			return false;
		}
		fov = fov2;
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
}
