using System;

namespace AnimatorLayerManagement
{
	[Serializable]
	public struct LayerRefId
	{
		public LayerRefId(int refId)
		{
			this.Value = refId;
		}

		public int Value;
	}
}
