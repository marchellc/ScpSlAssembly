using System.Collections.Generic;
using InventorySystem.Items.AutoIcons;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class KeycardIconGeneratorScene : AutoIconGeneratorScene, IIdentifierProvider
{
	[SerializeField]
	private Transform _gfxSpawnpoint;

	private GameObject _prevActive;

	private readonly Dictionary<KeycardGfx, KeycardGfx> _prevInstances = new Dictionary<KeycardGfx, KeycardGfx>();

	public ItemIdentifier ItemId { get; private set; }

	protected override void SetupScene(ItemBase item)
	{
		if (this._prevActive != null)
		{
			this._prevActive.SetActive(value: false);
		}
		KeycardItem keycardItem = item as KeycardItem;
		KeycardGfx keycardGfx = keycardItem.KeycardGfx;
		if (!this._prevInstances.TryGetValue(keycardGfx, out var value))
		{
			value = Object.Instantiate(keycardGfx, this._gfxSpawnpoint);
			value.SetAsIconSubject();
			this._prevInstances.Add(keycardGfx, value);
		}
		else
		{
			value.gameObject.SetActive(value: true);
		}
		this.ItemId = keycardItem.ItemId;
		if (this.ItemId.SerialNumber == 0)
		{
			KeycardDetailSynchronizer.ApplyTemplateDetails(keycardItem, value);
		}
		else
		{
			KeycardDetailSynchronizer.TryReapplyDetails(value);
		}
		this._prevActive = value.gameObject;
	}
}
