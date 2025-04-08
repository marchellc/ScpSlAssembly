using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	[PresetPrefabExtension("Sniper Scope Screen Viewmodel", false)]
	[PresetPrefabExtension("NV Scope Screen Viewmodel", false)]
	public class ViewmodelDualCamExtension : MonoBehaviour, IViewmodelExtension
	{
		public bool TryGetVerticalScreenOccupation(out float verticalScreenOccupation)
		{
			verticalScreenOccupation = 0f;
			Vector3 vector;
			if (!ViewmodelCamera.TryGetViewportPoint(this._topPointTr.position, out vector))
			{
				return false;
			}
			Vector3 vector2;
			if (!ViewmodelCamera.TryGetViewportPoint(this._bottomPointTr.position, out vector2))
			{
				return false;
			}
			verticalScreenOccupation = (vector - vector2).MagnitudeOnlyY();
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
			IAdsModule adsModule;
			if (!this._firearm.TryGetModule(out adsModule, true))
			{
				return;
			}
			float num;
			if (!this.TryGetVerticalScreenOccupation(out num))
			{
				return;
			}
			float adsAmount = adsModule.AdsAmount;
			this._matInstance.SetFloat(ViewmodelDualCamExtension.AdsHash, adsAmount);
			this._cameraSetupRoot.SetActive(adsAmount > 0f);
			Transform currentCamera = MainCameraController.CurrentCamera;
			this._cameraTr.SetPositionAndRotation(currentCamera.position, currentCamera.rotation);
			float num2 = this._zoomAmount / num;
			float num3 = 70f / num2;
			this._fovCamera.fieldOfView = num3;
		}

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
	}
}
