using System;
using AnimatorLayerManagement;
using PlayerRoles.FirstPersonControl.Thirdperson;

namespace InventorySystem.Items.Thirdperson
{
	[Serializable]
	public class ItemLayerLink
	{
		public int GetLayerIndex(AnimatedCharacterModel model)
		{
			if (this._cachedIndex == null)
			{
				this._cachedIndex = new int?(model.LayerManager.GetLayerIndex(this.LayerRef));
			}
			return this._cachedIndex.Value;
		}

		public AnimItemLayer3p Layer3p;

		public LayerRefId LayerRef;

		private int? _cachedIndex;
	}
}
