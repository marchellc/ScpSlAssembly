using System;
using InventorySystem.Items.Firearms.Modules;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public class NightVisionScopeAttachment : SerializableAttachment, ILightEmittingItem
	{
		public bool IsEmittingLight
		{
			get
			{
				IAdsModule adsModule;
				return this.IsEnabled && base.Firearm.TryGetModule(out adsModule, true) && adsModule.AdsAmount > 0.6f;
			}
		}

		private const float AdsThreshold = 0.6f;
	}
}
