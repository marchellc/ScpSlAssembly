using System;

namespace AnimatorLayerManagement
{
	[Serializable]
	public struct RefIdIndexPair
	{
		public RefIdIndexPair(LayerRefId refId, int layerIndex)
		{
			this.RefId = refId;
			this.LayerIndex = layerIndex;
		}

		public LayerRefId RefId;

		public int LayerIndex;
	}
}
