using System;
using AnimatorLayerManagement;
using PlayerRoles.FirstPersonControl.Thirdperson;

namespace InventorySystem.Items.Thirdperson;

[Serializable]
public class ItemLayerLink
{
	public AnimItemLayer3p Layer3p;

	public LayerRefId LayerRef;

	private int? _cachedIndex;

	public int GetLayerIndex(AnimatedCharacterModel model)
	{
		if (!this._cachedIndex.HasValue)
		{
			this._cachedIndex = model.LayerManager.GetLayerIndex(this.LayerRef);
		}
		return this._cachedIndex.Value;
	}
}
