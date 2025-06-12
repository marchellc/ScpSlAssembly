using System;
using InventorySystem.Items;
using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Crosshairs;

public class CrosshairController : MonoBehaviour
{
	private static CrosshairController _singleton;

	private static bool _singletonSet;

	[SerializeField]
	private MonoBehaviour _defaultCrosshair;

	[SerializeField]
	private MonoBehaviour[] _customCrosshairs;

	[SerializeField]
	private GameObject _rootObject;

	[SerializeField]
	private CanvasScaler _canvasScaler;

	private bool IsLowResolution
	{
		get
		{
			if (Screen.width < 1280)
			{
				return Screen.height < 720;
			}
			return false;
		}
	}

	private void Start()
	{
		Inventory.OnCurrentItemChanged += OnItemChanged;
		CrosshairController._singleton = this;
		CrosshairController._singletonSet = true;
	}

	private void OnDestroy()
	{
		Inventory.OnCurrentItemChanged -= OnItemChanged;
		CrosshairController._singletonSet = false;
	}

	private void Update()
	{
		this._rootObject.SetActive(ReferenceHub.TryGetLocalHub(out var hub) && hub.roleManager.CurrentRole is IHealthbarRole && hub.IsAlive() && !Cursor.visible);
		this.AdjustScaleMode();
	}

	private static void OnItemChanged(ReferenceHub ply, ItemIdentifier prevItem, ItemIdentifier newItem)
	{
		if (ply.isLocalPlayer)
		{
			CrosshairController.Refresh(ply, newItem.SerialNumber);
		}
	}

	public static void Refresh(ReferenceHub ply, ushort serial)
	{
		if (CrosshairController._singletonSet)
		{
			if (!ply.inventory.UserInventory.Items.TryGetValue(serial, out var value))
			{
				value = null;
			}
			Type type = ((value is ICustomCrosshairItem customCrosshairItem) ? customCrosshairItem.CrosshairType : null);
			MonoBehaviour[] customCrosshairs = CrosshairController._singleton._customCrosshairs;
			foreach (MonoBehaviour obj in customCrosshairs)
			{
				Type type2 = obj.GetType();
				obj.gameObject.SetActive(type2 == type);
			}
			CrosshairController._singleton._defaultCrosshair.gameObject.SetActive(type == null);
		}
	}

	private void AdjustScaleMode()
	{
		this._canvasScaler.uiScaleMode = ((!this.IsLowResolution) ? CanvasScaler.ScaleMode.ScaleWithScreenSize : CanvasScaler.ScaleMode.ConstantPixelSize);
	}
}
