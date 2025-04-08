using System;

namespace PlayerRoles.Spectating
{
	[Serializable]
	public struct SpectatableListSpawnedElement
	{
		public void ReturnToPool()
		{
			this.FullSize.ReturnToPool(true);
			this.Compact.ReturnToPool(true);
		}

		public int Priority;

		public SpectatableListElementBase FullSize;

		public SpectatableListElementBase Compact;

		public SpectatableModuleBase Target;
	}
}
