using System;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	[PresetPrefabExtension("Laser Viewmodel", false)]
	public class ViewmodelLaserExtension : MonoBehaviour, IViewmodelExtension, IDestroyExtensionReceiver
	{
		private float ClientAds
		{
			get
			{
				IAdsModule adsModule;
				if (!this._firearm.TryGetModule(out adsModule, true))
				{
					return 0f;
				}
				return adsModule.AdsAmount;
			}
		}

		private Vector3 BarrelForward
		{
			get
			{
				if (!this._hasBarrelTip)
				{
					return this._selfTr.parent.forward;
				}
				return this._barrelTip.WorldspaceDirection;
			}
		}

		private bool WantsToUnlock
		{
			get
			{
				foreach (ModuleBase moduleBase in this._firearm.Modules)
				{
					IInspectorModule inspectorModule = moduleBase as IInspectorModule;
					if (inspectorModule != null && inspectorModule.DisplayInspecting)
					{
						return true;
					}
					IBusyIndicatorModule busyIndicatorModule = moduleBase as IBusyIndicatorModule;
					if (busyIndicatorModule != null && busyIndicatorModule.IsBusy && !(busyIndicatorModule is IActionModule))
					{
						return true;
					}
				}
				return false;
			}
		}

		private void LateUpdate()
		{
			this._unlockBlend = Mathf.MoveTowards(this._unlockBlend, (float)(this.WantsToUnlock ? 1 : 0), Time.deltaTime * 5f);
			this._selfTr.localPosition = this._localPos;
			Vector3 forward = this._viewmodelTr.forward;
			Vector3 barrelForward = this.BarrelForward;
			Vector3 position = this._selfTr.position;
			float num = Vector3.Angle(forward, barrelForward);
			float num2 = Mathf.Clamp01(1f - this.ClientAds * 2.5f);
			float num3 = Mathf.InverseLerp(2.8f, 20f, num * num2);
			float num4 = Mathf.SmoothStep(0f, 1f, num3 + this._unlockBlend);
			float num5 = this._viewmodelTr.InverseTransformPoint(position).z;
			num5 = Mathf.Min(num5, this._distanceFromWall - 0.2f);
			Vector3 vector = new Vector3(0f, 0f, num5);
			Vector3 vector2 = this._viewmodelTr.TransformPoint(vector);
			this._selfTr.forward = Vector3.Slerp(forward, barrelForward, num4);
			this._selfTr.position = Vector3.Lerp(vector2, position, num4);
			this._lightSource.color = this._laserColor * num2;
		}

		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._firearm = viewmodel.ParentFirearm;
			this._viewmodelTr = viewmodel.transform;
			this._lightSource = base.GetComponent<Light>();
			this._laserColor = this._lightSource.color;
			this._selfTr = base.transform;
			this._localPos = this._selfTr.localPosition;
			this._hasBarrelTip = viewmodel.TryGetExtension<BarrelTipExtension>(out this._barrelTip);
		}

		public void OnDestroyExtension()
		{
		}

		private Light _lightSource;

		private Transform _selfTr;

		private Transform _viewmodelTr;

		private Firearm _firearm;

		private BarrelTipExtension _barrelTip;

		private Color _laserColor;

		private Vector3 _localPos;

		private bool _hasBarrelTip;

		private float _distanceFromWall;

		private float _unlockBlend;

		private const float MinDistanceFromWall = 0.2f;

		private const float MaxDistanceFromWall = 15f;

		private const float AdsMultiplier = 2.5f;

		private const float UnlockSpeed = 5f;

		private const float AngularSnapMin = 2.8f;

		private const float AngularSnapMax = 20f;
	}
}
