using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

[PresetPrefabExtension("Sniper Scope Screen Viewmodel", false)]
[PresetPrefabExtension("NV Scope Screen Viewmodel", false)]
public class ViewmodelDualCamExtension : MonoBehaviour, IViewmodelExtension
{
	private static readonly Dictionary<Material, Material> MaterialInstances = new Dictionary<Material, Material>();

	private static readonly int RenderTexHash = Shader.PropertyToID("_RenderTex");

	private static readonly int AdsHash = Shader.PropertyToID("_Ads");

	[SerializeField]
	private Renderer _targetRenderer;

	[SerializeField]
	private GameObject _cameraSetupRoot;

	[SerializeField]
	private float _zoomAmount = 1f;

	[SerializeField]
	private Transform _topPointTr;

	[SerializeField]
	private Transform _bottomPointTr;

	private Camera _fovCamera;

	private Firearm _firearm;

	private Material _matInstance;

	private Transform _cameraTr;

	public bool TryGetVerticalScreenOccupation(out float verticalScreenOccupation)
	{
		verticalScreenOccupation = 0f;
		if (!ViewmodelCamera.TryGetViewportPoint(this._topPointTr.position, out var viewport))
		{
			return false;
		}
		if (!ViewmodelCamera.TryGetViewportPoint(this._bottomPointTr.position, out var viewport2))
		{
			return false;
		}
		verticalScreenOccupation = (viewport - viewport2).MagnitudeOnlyY();
		return true;
	}

	public virtual void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._fovCamera = this._cameraSetupRoot.GetComponent<Camera>();
		this._cameraTr = this._cameraSetupRoot.transform;
		this._firearm = viewmodel.ParentFirearm;
		Material sharedMat = this._targetRenderer.sharedMaterial;
		this._matInstance = ViewmodelDualCamExtension.MaterialInstances.GetOrAdd(sharedMat, () => new Material(sharedMat));
		this._matInstance.SetTexture(ViewmodelDualCamExtension.RenderTexHash, this._fovCamera.targetTexture);
		this._targetRenderer.sharedMaterial = this._matInstance;
	}

	protected virtual void LateUpdate()
	{
		if (this._firearm.TryGetModule<IAdsModule>(out var module) && this.TryGetVerticalScreenOccupation(out var verticalScreenOccupation))
		{
			float adsAmount = module.AdsAmount;
			this._matInstance.SetFloat(ViewmodelDualCamExtension.AdsHash, adsAmount);
			this._cameraSetupRoot.SetActive(adsAmount > 0f);
			Transform currentCamera = MainCameraController.CurrentCamera;
			this._cameraTr.SetPositionAndRotation(currentCamera.position, currentCamera.rotation);
			float num = this._zoomAmount / verticalScreenOccupation;
			float fieldOfView = 70f / num;
			this._fovCamera.fieldOfView = fieldOfView;
		}
	}
}
