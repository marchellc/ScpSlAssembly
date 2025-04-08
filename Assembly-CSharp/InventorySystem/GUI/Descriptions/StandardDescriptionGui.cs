using System;
using InventorySystem.Items;
using TMPro;
using UnityEngine;

namespace InventorySystem.GUI.Descriptions
{
	public class StandardDescriptionGui : RadialDescriptionBase
	{
		public override void UpdateInfo(ItemBase targetItem, Color roleColor)
		{
			IItemDescription itemDescription = targetItem as IItemDescription;
			if (itemDescription != null)
			{
				this._title.text = itemDescription.Name;
				this._desc.text = itemDescription.Description;
				return;
			}
			TMP_Text title = this._title;
			IItemNametag itemNametag = targetItem as IItemNametag;
			title.text = ((itemNametag != null) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
			this._desc.text = string.Empty;
			Debug.LogError(string.Concat(new string[]
			{
				"Item '",
				targetItem.ItemTypeId.ToString(),
				"' of the class '",
				targetItem.GetType().FullName,
				"' does have an implementation of the 'IItemDescription' interface, which is required by items of the '",
				targetItem.Category.ToString(),
				"' category."
			}));
		}

		[SerializeField]
		private TextMeshProUGUI _title;

		[SerializeField]
		private TextMeshProUGUI _desc;
	}
}
