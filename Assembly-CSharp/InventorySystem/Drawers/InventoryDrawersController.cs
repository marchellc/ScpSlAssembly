using System;
using InventorySystem.Items;
using PlayerRoles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Drawers
{
	public class InventoryDrawersController : MonoBehaviour
	{
		private void Start()
		{
			Inventory.OnCurrentItemChanged += this.ItemChanged;
			PlayerRoleManager.OnRoleChanged += this.RecolorDetails;
		}

		private void OnDestroy()
		{
			Inventory.OnCurrentItemChanged -= this.ItemChanged;
			PlayerRoleManager.OnRoleChanged -= this.RecolorDetails;
		}

		private void RecolorDetails(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			this._roleColor = newRole.RoleColor;
			foreach (Graphic graphic in this._detailsToRecolor)
			{
				Color roleColor = this._roleColor;
				roleColor.a = graphic.color.a;
				graphic.color = roleColor;
			}
		}

		private void Update()
		{
			if (!this._active)
			{
				return;
			}
			float num = 1f - this._inventoryGroup.alpha;
			if (num != this._thisGroup.alpha)
			{
				this._thisGroup.alpha = num;
			}
			TMP_Text alertText = this._alertText;
			IItemAlertDrawer alertToTrack = this._alertToTrack;
			alertText.text = ((alertToTrack != null) ? alertToTrack.Alert.ParseText(this._roleColor) : null) ?? string.Empty;
			if (this._progressbarToTrack != null)
			{
				if (this._progressbarToTrack.ProgressbarEnabled)
				{
					this._progressbarSlider.gameObject.SetActive(true);
					this._progressbarSlider.minValue = this._progressbarToTrack.ProgressbarMin;
					this._progressbarSlider.maxValue = this._progressbarToTrack.ProgressbarMax;
					this._progressbarSlider.value = this._progressbarToTrack.ProgressbarValue;
					this._progressbarSliderRect.sizeDelta = new Vector2(this._progressbarToTrack.ProgressbarWidth, 10f);
					return;
				}
				this._progressbarSlider.gameObject.SetActive(false);
			}
		}

		private void ItemChanged(ReferenceHub hub, ItemIdentifier prevItem, ItemIdentifier newItem)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			ItemBase itemBase;
			this._active = hub.inventory.UserInventory.Items.TryGetValue(newItem.SerialNumber, out itemBase) && itemBase is IItemDrawer;
			this._alertToTrack = itemBase as IItemAlertDrawer;
			this._alertText.text = string.Empty;
			this._progressbarToTrack = itemBase as IItemProgressbarDrawer;
			this._progressbarSlider.gameObject.SetActive(false);
		}

		[SerializeField]
		private TextMeshProUGUI _alertText;

		[SerializeField]
		private Slider _progressbarSlider;

		[SerializeField]
		private RectTransform _progressbarSliderRect;

		[SerializeField]
		private Graphic[] _detailsToRecolor;

		[SerializeField]
		private CanvasGroup _thisGroup;

		[SerializeField]
		private CanvasGroup _inventoryGroup;

		private IItemAlertDrawer _alertToTrack;

		private IItemProgressbarDrawer _progressbarToTrack;

		private bool _active;

		private Color _roleColor;
	}
}
