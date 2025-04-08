using System;
using InventorySystem.Items.Firearms.Modules.Misc;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public class BuckshotPatternAttachment : SerializableAttachment
	{
		public BuckshotSettings Pattern { get; private set; }
	}
}
