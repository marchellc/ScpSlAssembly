using System.Collections.Generic;
using InventorySystem.Items;
using TMPro;
using UnityEngine;

namespace InventorySystem.GUI.Descriptions;

public class CustomDescriptionGui : RadialDescriptionBase
{
	private readonly Dictionary<CustomDescriptionGui, CustomDescriptionGui> _instancesByPrefab = new Dictionary<CustomDescriptionGui, CustomDescriptionGui>();

	private readonly List<CustomDescriptionGui> _allInstances = new List<CustomDescriptionGui>();

	[SerializeField]
	private TextMeshProUGUI _title;

	[SerializeField]
	private TextMeshProUGUI _description;

	[SerializeField]
	private TextMeshProUGUI[] _dynamicTexts;

	public override void UpdateInfo(ItemBase targetItem, Color roleColor)
	{
		if (!(targetItem is ICustomDescriptionItem customDescriptionItem))
		{
			Debug.LogError(string.Format("Item {0} must implement {1} in order to use {2}.", targetItem.ItemTypeId, "ICustomDescriptionItem", "CustomDescriptionGui"));
			return;
		}
		this.DisableAllInstances();
		this.GetOrAdd(customDescriptionItem.CustomGuiPrefab).UpdateInstance(targetItem.ItemTypeId, customDescriptionItem);
	}

	private void DisableAllInstances()
	{
		foreach (CustomDescriptionGui allInstance in this._allInstances)
		{
			allInstance.gameObject.SetActive(value: false);
		}
	}

	private CustomDescriptionGui GetOrAdd(CustomDescriptionGui template)
	{
		if (this._instancesByPrefab.TryGetValue(template, out var value))
		{
			return value;
		}
		CustomDescriptionGui customDescriptionGui = Object.Instantiate(template, base.transform);
		this._allInstances.Add(customDescriptionGui);
		this._instancesByPrefab[template] = customDescriptionGui;
		return customDescriptionGui;
	}

	private void UpdateInstance(ItemType itemType, ICustomDescriptionItem icd)
	{
		base.gameObject.SetActive(value: true);
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
			if (customDescriptionContent.TryGet(i, out var element))
			{
				this._dynamicTexts[i].text = element;
			}
		}
	}
}
