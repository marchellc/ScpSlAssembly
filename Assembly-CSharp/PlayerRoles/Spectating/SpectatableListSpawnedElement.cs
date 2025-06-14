using System;

namespace PlayerRoles.Spectating;

[Serializable]
public struct SpectatableListSpawnedElement
{
	public int Priority;

	public SpectatableListElementBase FullSize;

	public SpectatableListElementBase Compact;

	public SpectatableModuleBase Target;

	public void ReturnToPool()
	{
		this.FullSize.ReturnToPool();
		this.Compact.ReturnToPool();
	}
}
