using System;
using System.Collections.Generic;
using InventorySystem.Items;
using TMPro;
using UnityEngine;

namespace InventorySystem.GUI.Descriptions
{
	public class CustomDescriptionGui : RadialDescriptionBase
	{
		public override void UpdateInfo(ItemBase targetItem, Color roleColor)
		{
			ICustomDescriptionItem customDescriptionItem = targetItem as ICustomDescriptionItem;
			if (customDescriptionItem == null)
			{
				Debug.LogError(string.Format("Item {0} must implement {1} in order to use {2}.", targetItem.ItemTypeId, "ICustomDescriptionItem", "CustomDescriptionGui"));
				return;
			}
			this.DisableAllInstances();
			this.GetOrAdd(customDescriptionItem.CustomGuiPrefab).UpdateInstance(targetItem.ItemTypeId, customDescriptionItem);
		}

		private void DisableAllInstances()
		{
			foreach (CustomDescriptionGui customDescriptionGui in this._allInstances)
			{
				customDescriptionGui.gameObject.SetActive(false);
			}
		}

		private CustomDescriptionGui GetOrAdd(CustomDescriptionGui template)
		{
			CustomDescriptionGui customDescriptionGui;
			if (this._instancesByPrefab.TryGetValue(template, out customDescriptionGui))
			{
				return customDescriptionGui;
			}
			CustomDescriptionGui customDescriptionGui2 = global::UnityEngine.Object.Instantiate<CustomDescriptionGui>(template, base.transform);
			this._allInstances.Add(customDescriptionGui2);
			this._instancesByPrefab[template] = customDescriptionGui2;
			return customDescriptionGui2;
		}

		private void UpdateInstance(ItemType itemType, ICustomDescriptionItem icd)
		{
			base.gameObject.SetActive(true);
			if (this._title != null)
			{
				this._title.text = itemType.GetName();
			}
			if (this._description != null)
			{
				this._description.text = itemType.GetDescription();
			}
			string[] customDescriptionContent = icd.CustomDescriptionContent;
			for (int i = 0; i < this._dynamicTexts.Length; i++)
			{
				string text;
				if (customDescriptionContent.TryGet(i, out text))
				{
					this._dynamicTexts[i].text = text;
				}
			}
		}

		private readonly Dictionary<CustomDescriptionGui, CustomDescriptionGui> _instancesByPrefab = new Dictionary<CustomDescriptionGui, CustomDescriptionGui>();

		private readonly List<CustomDescriptionGui> _allInstances = new List<CustomDescriptionGui>();

		[SerializeField]
		private TextMeshProUGUI _title;

		[SerializeField]
		private TextMeshProUGUI _description;

		[SerializeField]
		private TextMeshProUGUI[] _dynamicTexts;
	}
}
