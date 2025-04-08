using System;
using InventorySystem.Items;
using TMPro;
using UnityEngine;

namespace InventorySystem.GUI.Descriptions
{
	public class EmptyDescriptionGui : RadialDescriptionBase
	{
		public override void UpdateInfo(ItemBase targetItem, Color roleColor)
		{
			TMP_Text desc = this._desc;
			IItemNametag itemNametag = targetItem as IItemNametag;
			desc.text = ((itemNametag != null) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
		}

		[SerializeField]
		private TextMeshProUGUI _desc;
	}
}
