using System;
using InventorySystem.Items;
using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Crosshairs
{
	public class CrosshairController : MonoBehaviour
	{
		private bool IsLowResolution
		{
			get
			{
				return Screen.width < 1280 && Screen.height < 720;
			}
		}

		private void Start()
		{
			Inventory.OnCurrentItemChanged += CrosshairController.OnItemChanged;
			CrosshairController._singleton = this;
			CrosshairController._singletonSet = true;
		}

		private void OnDestroy()
		{
			Inventory.OnCurrentItemChanged -= CrosshairController.OnItemChanged;
			CrosshairController._singletonSet = false;
		}

		private void Update()
		{
			ReferenceHub referenceHub;
			this._rootObject.SetActive(ReferenceHub.TryGetLocalHub(out referenceHub) && referenceHub.roleManager.CurrentRole is IHealthbarRole && referenceHub.IsAlive() && !Cursor.visible);
			this.AdjustScaleMode();
		}

		private static void OnItemChanged(ReferenceHub ply, ItemIdentifier prevItem, ItemIdentifier newItem)
		{
			if (!ply.isLocalPlayer)
			{
				return;
			}
			CrosshairController.Refresh(ply, newItem.SerialNumber);
		}

		public static void Refresh(ReferenceHub ply, ushort serial)
		{
			if (!CrosshairController._singletonSet)
			{
				return;
			}
			ItemBase itemBase;
			if (!ply.inventory.UserInventory.Items.TryGetValue(serial, out itemBase))
			{
				itemBase = null;
			}
			ICustomCrosshairItem customCrosshairItem = itemBase as ICustomCrosshairItem;
			Type type = ((customCrosshairItem != null) ? customCrosshairItem.CrosshairType : null);
			foreach (MonoBehaviour monoBehaviour in CrosshairController._singleton._customCrosshairs)
			{
				Type type2 = monoBehaviour.GetType();
				monoBehaviour.gameObject.SetActive(type2 == type);
			}
			CrosshairController._singleton._defaultCrosshair.gameObject.SetActive(type == null);
		}

		private void AdjustScaleMode()
		{
			this._canvasScaler.uiScaleMode = (this.IsLowResolution ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize);
		}

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
	}
}
