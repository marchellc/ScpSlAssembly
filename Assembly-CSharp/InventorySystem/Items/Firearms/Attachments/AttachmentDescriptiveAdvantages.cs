﻿using System;

namespace InventorySystem.Items.Firearms.Attachments
{
	[Flags]
	public enum AttachmentDescriptiveAdvantages
	{
		None = 0,
		Flashlight = 2,
		AmmoCounter = 4,
		FlashSuppression = 8,
		NightVision = 16,
		ShotgunShells = 32
	}
}
