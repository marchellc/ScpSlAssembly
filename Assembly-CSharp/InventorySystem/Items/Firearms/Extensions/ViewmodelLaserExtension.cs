using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

[PresetPrefabExtension("Laser Viewmodel", false)]
public class ViewmodelLaserExtension : MonoBehaviour, IViewmodelExtension, IDestroyExtensionReceiver
{
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

	private float ClientAds
	{
		get
		{
			if (!this._firearm.TryGetModule<IAdsModule>(out var module))
			{
				return 0f;
			}
			return module.AdsAmount;
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
			ModuleBase[] modules = this._firearm.Modules;
			foreach (ModuleBase moduleBase in modules)
			{
				if (moduleBase is IInspectorModule { DisplayInspecting: not false })
				{
					return true;
				}
				if (moduleBase is IBusyIndicatorModule { IsBusy: not false } busyIndicatorModule && !(busyIndicatorModule is IActionModule))
				{
					return true;
				}
			}
			return false;
		}
	}

	private void LateUpdate()
	{
		this._unlockBlend = Mathf.MoveTowards(this._unlockBlend, this.WantsToUnlock ? 1 : 0, Time.deltaTime * 5f);
		this._selfTr.localPosition = this._localPos;
		Vector3 forward = this._viewmodelTr.forward;
		Vector3 barrelForward = this.BarrelForward;
		Vector3 position = this._selfTr.position;
		float num = Vector3.Angle(forward, barrelForward);
		float num2 = Mathf.Clamp01(1f - this.ClientAds * 2.5f);
		float num3 = Mathf.InverseLerp(2.8f, 20f, num * num2);
		float t = Mathf.SmoothStep(0f, 1f, num3 + this._unlockBlend);
		float z = this._viewmodelTr.InverseTransformPoint(position).z;
		z = Mathf.Min(z, this._distanceFromWall - 0.2f);
		Vector3 position2 = new Vector3(0f, 0f, z);
		Vector3 a = this._viewmodelTr.TransformPoint(position2);
		this._selfTr.forward = Vector3.Slerp(forward, barrelForward, t);
		this._selfTr.position = Vector3.Lerp(a, position, t);
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
}
