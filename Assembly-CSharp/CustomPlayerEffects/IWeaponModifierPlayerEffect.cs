using System;
using InventorySystem.Items.Firearms.Attachments;

namespace CustomPlayerEffects
{
	public interface IWeaponModifierPlayerEffect
	{
		bool TryGetWeaponParam(AttachmentParam param, out float val);

		bool ParamsActive { get; }
	}
}
