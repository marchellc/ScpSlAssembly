using System;
using System.Collections.Generic;
using InventorySystem.Items;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace CameraShaking
{
	public class CameraShakeController : MonoBehaviour
	{
		public static void AddEffect(IShakeEffect effect)
		{
			CameraShakeController.Singleton._effects.Add(effect);
		}

		public float CamerasFOV
		{
			get
			{
				return this._camera.fieldOfView;
			}
			private set
			{
				this._camera.fieldOfView = value;
			}
		}

		private void Start()
		{
			this._startPos = base.transform.localPosition;
			CameraShakeController.Singleton = this;
			this._viewmodelRoot = base.GetComponentInChildren<SharedHandsController>().transform;
		}

		private void OnDisable()
		{
			this._effects.Clear();
		}

		private void LateUpdate()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			if (this._effects.Count == 0)
			{
				IZoomModifyingItem zoomModifyingItem = referenceHub.inventory.CurInstance as IZoomModifyingItem;
				if (zoomModifyingItem != null)
				{
					this.CamerasFOV = 70f / zoomModifyingItem.ZoomAmount;
				}
				return;
			}
			HashSet<float> hashSet = HashSetPool<float>.Shared.Rent();
			Quaternion quaternion = Quaternion.identity;
			Quaternion quaternion2 = Quaternion.identity;
			Vector3 vector = this._startPos;
			for (int i = this._effects.Count - 1; i >= 0; i--)
			{
				ShakeEffectValues shakeEffectValues;
				if (!this._effects[i].GetEffect(referenceHub, out shakeEffectValues))
				{
					this._effects.RemoveAt(i);
				}
				else
				{
					if (shakeEffectValues.FovPercent != 1f)
					{
						hashSet.Add(shakeEffectValues.FovPercent);
					}
					FpcMouseLook fpcMouseLook;
					if (FpcMouseLook.TryGetLocalMouseLook(out fpcMouseLook))
					{
						if (shakeEffectValues.HorizontalMouseLook != 0f)
						{
							fpcMouseLook.CurrentHorizontal += shakeEffectValues.HorizontalMouseLook;
						}
						if (shakeEffectValues.VerticalMouseLook != 0f)
						{
							fpcMouseLook.CurrentVertical += shakeEffectValues.VerticalMouseLook;
						}
					}
					quaternion *= shakeEffectValues.RootCameraRotation;
					vector += shakeEffectValues.RootCameraPositionOffset;
					quaternion2 *= shakeEffectValues.ViewmodelCameraRotation;
				}
			}
			if (hashSet.Count > 0)
			{
				float num = 0f;
				foreach (float num2 in hashSet)
				{
					num += num2 - 1f;
				}
				this.CamerasFOV = 70f * (num + 1f);
			}
			else
			{
				this.CamerasFOV = 70f;
			}
			IZoomModifyingItem zoomModifyingItem2 = referenceHub.inventory.CurInstance as IZoomModifyingItem;
			if (zoomModifyingItem2 != null)
			{
				this.CamerasFOV /= zoomModifyingItem2.ZoomAmount;
			}
			HashSetPool<float>.Shared.Return(hashSet);
			base.transform.SetLocalPositionAndRotation(Vector3.zero, quaternion);
			base.transform.position += vector;
			this._viewmodelRoot.localRotation = quaternion2;
		}

		[SerializeField]
		private Camera _camera;

		private Vector3 _startPos;

		private Transform _viewmodelRoot;

		private readonly List<IShakeEffect> _effects = new List<IShakeEffect>();

		public static CameraShakeController Singleton;
	}
}
