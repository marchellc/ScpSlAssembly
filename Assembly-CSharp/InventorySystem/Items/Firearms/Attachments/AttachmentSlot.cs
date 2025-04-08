using System;

namespace InventorySystem.Items.Firearms.Attachments
{
	public enum AttachmentSlot : byte
	{
		Sight,
		Barrel,
		SideRail,
		BottomRail,
		Ammunition,
		Stock,
		Stability,
		Body,
		Unassigned = 255
	}
}
