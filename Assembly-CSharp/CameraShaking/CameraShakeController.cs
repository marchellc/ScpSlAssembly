using System.Collections.Generic;
using InventorySystem.Items;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace CameraShaking;

public class CameraShakeController : MonoBehaviour
{
	[SerializeField]
	private Camera _camera;

	private Vector3 _startPos;

	private Transform _viewmodelRoot;

	private readonly List<IShakeEffect> _effects = new List<IShakeEffect>();

	public static CameraShakeController Singleton;

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

	public static void AddEffect(IShakeEffect effect)
	{
		CameraShakeController.Singleton._effects.Add(effect);
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
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return;
		}
		if (this._effects.Count == 0)
		{
			if (hub.inventory.CurInstance is IZoomModifyingItem zoomModifyingItem)
			{
				this.CamerasFOV = 70f / zoomModifyingItem.ZoomAmount;
			}
			return;
		}
		HashSet<float> hashSet = HashSetPool<float>.Shared.Rent();
		Quaternion identity = Quaternion.identity;
		Quaternion identity2 = Quaternion.identity;
		Vector3 startPos = this._startPos;
		for (int num = this._effects.Count - 1; num >= 0; num--)
		{
			if (!this._effects[num].GetEffect(hub, out var shakeValues))
			{
				this._effects.RemoveAt(num);
			}
			else
			{
				if (shakeValues.FovPercent != 1f)
				{
					hashSet.Add(shakeValues.FovPercent);
				}
				if (FpcMouseLook.TryGetLocalMouseLook(out var ml))
				{
					if (shakeValues.HorizontalMouseLook != 0f)
					{
						ml.CurrentHorizontal += shakeValues.HorizontalMouseLook;
					}
					if (shakeValues.VerticalMouseLook != 0f)
					{
						ml.CurrentVertical += shakeValues.VerticalMouseLook;
					}
				}
				identity *= shakeValues.RootCameraRotation;
				startPos += shakeValues.RootCameraPositionOffset;
				identity2 *= shakeValues.ViewmodelCameraRotation;
			}
		}
		if (hashSet.Count > 0)
		{
			float num2 = 0f;
			foreach (float item in hashSet)
			{
				num2 += item - 1f;
			}
			this.CamerasFOV = 70f * (num2 + 1f);
		}
		else
		{
			this.CamerasFOV = 70f;
		}
		if (hub.inventory.CurInstance is IZoomModifyingItem zoomModifyingItem2)
		{
			this.CamerasFOV /= zoomModifyingItem2.ZoomAmount;
		}
		HashSetPool<float>.Shared.Return(hashSet);
		base.transform.SetLocalPositionAndRotation(Vector3.zero, identity);
		base.transform.position += startPos;
		this._viewmodelRoot.localRotation = identity2;
	}
}
