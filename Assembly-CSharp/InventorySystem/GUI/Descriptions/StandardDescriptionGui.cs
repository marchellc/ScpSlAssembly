using InventorySystem.Items;
using TMPro;
using UnityEngine;

namespace InventorySystem.GUI.Descriptions;

public class StandardDescriptionGui : RadialDescriptionBase
{
	[SerializeField]
	private TextMeshProUGUI _title;

	[SerializeField]
	private TextMeshProUGUI _desc;

	public override void UpdateInfo(ItemBase targetItem, Color roleColor)
	{
		if (targetItem is IItemDescription itemDescription)
		{
			this._title.text = itemDescription.Name;
			this._desc.text = itemDescription.Description;
			return;
		}
		this._title.text = ((targetItem is IItemNametag itemNametag) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
		this._desc.text = string.Empty;
		Debug.LogError("Item '" + targetItem.ItemTypeId.ToString() + "' of the class '" + targetItem.GetType().FullName + "' does have an implementation of the 'IItemDescription' interface, which is required by items of the '" + targetItem.Category.ToString() + "' category.");
	}
}
