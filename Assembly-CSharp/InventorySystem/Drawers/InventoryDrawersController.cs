using InventorySystem.Items;
using PlayerRoles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Drawers;

public class InventoryDrawersController : MonoBehaviour
{
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

	private void Start()
	{
		Inventory.OnCurrentItemChanged += ItemChanged;
		PlayerRoleManager.OnRoleChanged += RecolorDetails;
	}

	private void OnDestroy()
	{
		Inventory.OnCurrentItemChanged -= ItemChanged;
		PlayerRoleManager.OnRoleChanged -= RecolorDetails;
	}

	private void RecolorDetails(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (hub.isLocalPlayer)
		{
			this._roleColor = newRole.RoleColor;
			Graphic[] detailsToRecolor = this._detailsToRecolor;
			foreach (Graphic graphic in detailsToRecolor)
			{
				Color roleColor = this._roleColor;
				roleColor.a = graphic.color.a;
				graphic.color = roleColor;
			}
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
		this._alertText.text = this._alertToTrack?.Alert.ParseText(this._roleColor) ?? string.Empty;
		if (this._progressbarToTrack != null)
		{
			if (this._progressbarToTrack.ProgressbarEnabled)
			{
				this._progressbarSlider.gameObject.SetActive(value: true);
				this._progressbarSlider.minValue = this._progressbarToTrack.ProgressbarMin;
				this._progressbarSlider.maxValue = this._progressbarToTrack.ProgressbarMax;
				this._progressbarSlider.value = this._progressbarToTrack.ProgressbarValue;
				this._progressbarSliderRect.sizeDelta = new Vector2(this._progressbarToTrack.ProgressbarWidth, 10f);
			}
			else
			{
				this._progressbarSlider.gameObject.SetActive(value: false);
			}
		}
	}

	private void ItemChanged(ReferenceHub hub, ItemIdentifier prevItem, ItemIdentifier newItem)
	{
		if (hub.isLocalPlayer)
		{
			this._active = hub.inventory.UserInventory.Items.TryGetValue(newItem.SerialNumber, out var value) && value is IItemDrawer;
			this._alertToTrack = value as IItemAlertDrawer;
			this._alertText.text = string.Empty;
			this._progressbarToTrack = value as IItemProgressbarDrawer;
			this._progressbarSlider.gameObject.SetActive(value: false);
		}
	}
}
