using System;

namespace AnimatorLayerManagement;

[Serializable]
public struct LayerRefId
{
	public int Value;

	public LayerRefId(int refId)
	{
		this.Value = refId;
	}
}
