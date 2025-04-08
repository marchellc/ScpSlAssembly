using System;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.GUI.Descriptions
{
	public class KeycardDescriptionGui : RadialDescriptionBase
	{
		public override void UpdateInfo(ItemBase targetItem, Color roleColor)
		{
			TMP_Text title = this._title;
			IItemNametag itemNametag = targetItem as IItemNametag;
			title.text = ((itemNametag != null) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
			KeycardItem keycardItem = targetItem as KeycardItem;
			if (keycardItem != null)
			{
				foreach (KeycardDescriptionGui.PermissionNode permissionNode in this._nodes)
				{
					permissionNode.SetColor(keycardItem.Permissions, roleColor);
				}
			}
		}

		[SerializeField]
		private TextMeshProUGUI _title;

		[SerializeField]
		private KeycardDescriptionGui.PermissionNode[] _nodes;

		[Serializable]
		private struct PermissionNode
		{
			public void SetColor(KeycardPermissions keycardPerms, Color roleColor)
			{
				Color color = roleColor;
				color.a = (keycardPerms.HasFlagFast(this._permission) ? 1f : 0.2f);
				this._node.color = color;
			}

			[SerializeField]
			private Image _node;

			[SerializeField]
			private KeycardPermissions _permission;
		}
	}
}
